//! Windows x64 trampolines
//!
//! The entry of a freshly compiled method is rewritten into a jump to a trampoline allocated inside the same two
//! gigabyte window. The trampoline reads a per-thread soft limit, compares it with the stack pointer and either replays
//! the instructions it displaced or jumps to the managed throw entry. The fast path never leaves the trampoline and
//! touches one scratch register, so a call costs a handful of native instructions.
//!
//! The displaced instructions are decoded and re-encoded with iced-x86; the block encoder fixes every relative branch
//! and every rip-relative operand for the new address. A body whose first instructions cannot be relocated is left
//! alone, which keeps the hook from ever changing behavior it cannot reproduce.

use std::collections::HashSet;
use std::ffi::c_void;
use std::ptr;
use std::sync::atomic::{AtomicBool, AtomicU64, Ordering};
use std::sync::{Mutex, OnceLock};
use std::time::Instant;

use iced_x86::{
    BlockEncoder, BlockEncoderOptions, Decoder, DecoderOptions, FlowControl, Instruction,
    InstructionBlock,
};

use crate::logging;

//How many bytes of the entry are read at most; an entry is a prologue, not a body
const MAX_ENTRY_BYTES: usize = 32;

//How many instructions may be displaced before the method is given up on
const MAX_STOLEN: usize = 8;

//One trampoline fits well inside this; the size is fixed so a chunk can be carved without knowing the encoding first
const SLOT_SIZE: usize = 512;

//A chunk is one VirtualAlloc call, carved into slots
const CHUNK_SIZE: usize = 64 * 1024;

const MEM_COMMIT: u32 = 0x1000;
const MEM_RESERVE: u32 = 0x2000;
const PAGE_EXECUTE_READWRITE: u32 = 0x40;
const TLS_OUT_OF_INDEXES: u32 = 0xFFFF_FFFF;

//ThreadLocalStoragePointer inside the x64 TEB
const TEB_TLS_POINTER: usize = 0x58;

//Room above the soft limit that has to be regained before the guard speaks again
//The throw, its unwind and the catch all run while the thread is below the limit, so they need the quiet period
const REARM_GRACE: usize = 64 * 1024;

//A thread with less stack than this is not guarded; half of a tiny stack is worse than no check
const MIN_GUARDED_STACK: usize = 128 * 1024;

//How far a relative jump reaches, with room left for the distance the slot itself consumes
const NEAR_LIMIT: usize = 0x7FFF_0000;

/// Per-thread state the trampoline's first field points at
///
/// `limit` is the stack pointer the check compares against and is the only field the fast path reads. `normal_limit` is
/// the value to put back once a trip has been handled; while `quiet` is set the limit is pushed to the top of the
/// address space, which sends every entry down the slow path without adding a second compare to the hot path.
#[repr(C)]
struct ThreadState {
    limit: usize,
    quiet: usize,
    normal_limit: usize,
}

static TLS_INDEX: OnceLock<u32> = OnceLock::new();
static HOOKED_ADDRESSES: OnceLock<Mutex<HashSet<usize>>> = OnceLock::new();
static CHUNKS: OnceLock<Mutex<Vec<Chunk>>> = OnceLock::new();
static FIRST_HOOK: AtomicBool = AtomicBool::new(false);
static VIRTUAL_ALLOC2: OnceLock<Option<VirtualAlloc2>> = OnceLock::new();
static TRACE: OnceLock<bool> = OnceLock::new();

//Two entries often share a code page, and JITCompilationFinished arrives on several JIT threads; without this the
//protection changes of two installs could interleave and one of them would write into a page already restored to RX
static PATCH_LOCK: Mutex<()> = Mutex::new(());

struct Chunk {
    base: usize,
    used: usize,
}

type VirtualAlloc2 = unsafe extern "system" fn(
    process: *mut c_void,
    base_address: *mut c_void,
    size: usize,
    allocation_type: u32,
    protection: u32,
    parameters: *mut MemExtendedParameter,
    parameter_count: u32,
) -> *mut c_void;

//MEM_EXTENDED_PARAMETER holds an eight byte union, so the struct is padded to sixteen
#[repr(C)]
struct MemExtendedParameter {
    kind: u32,
    reserved: u32,
    value: usize,
}

#[repr(C)]
struct MemAddressRequirements {
    lowest: usize,
    highest: usize,
    alignment: usize,
}

extern "system" {
    fn GetCurrentThreadStackLimits(low: *mut usize, high: *mut usize);
    fn TlsAlloc() -> u32;
    fn TlsGetValue(index: u32) -> *mut c_void;
    fn TlsSetValue(index: u32, value: *mut c_void) -> i32;
    fn VirtualAlloc(address: *mut c_void, size: usize, allocation_type: u32, protect: u32) -> *mut c_void;
    fn VirtualProtect(address: *mut c_void, size: usize, new_protect: u32, old_protect: *mut u32) -> i32;
    fn FlushInstructionCache(process: *mut c_void, address: *const c_void, size: usize) -> i32;
    fn GetCurrentProcess() -> *mut c_void;
    fn GetModuleHandleA(name: *const u8) -> *mut c_void;
    fn GetProcAddress(module: *mut c_void, name: *const u8) -> *mut c_void;

    //Defined by the assembly stub below; the trampoline reaches it through a pointer stored in its own page
    fn ncn_stack_slow_entry(rsp_at_entry: usize) -> usize;
}

//The stub exists so the indirect call from the trampoline lands on an endbr64 even when CET only knows about the
//targets the compiler instrumented
core::arch::global_asm!(
    ".text",
    ".p2align 4",
    ".globl ncn_stack_slow_entry",
    "ncn_stack_slow_entry:",
    "endbr64",
    "jmp {impl}",
    impl = sym slow_entry_impl,
);

/// slow_entry_impl decides between replaying the prologue and throwing
///
/// It runs with almost no stack left and must not allocate, log or unwind: it only reads the thread state and answers
/// the address the trampoline should continue at, where zero means "replay the displaced instructions".
extern "C" fn slow_entry_impl(rsp_at_entry: usize) -> usize {
    super::note_slow_call();

    let Some(state) = (unsafe { thread_state(rsp_at_entry) }) else {
        return 0;
    };

    if state.limit == 0 {
        return 0;
    }

    //A thread already handling a trip passes every entry until the stack has climbed back out of the reserve
    if state.quiet != 0 {
        if rsp_at_entry > state.quiet {
            state.quiet = 0;
            state.limit = state.normal_limit;
        } else {
            return 0;
        }
    }

    if rsp_at_entry >= state.limit {
        return 0;
    }

    let trip_entry = super::trip_entry();
    if trip_entry == 0 {
        return 0;
    }

    //Pushing the limit to the top of the address space keeps every entry coming here while the trip is handled
    state.quiet = state.normal_limit.saturating_add(REARM_GRACE);
    state.limit = usize::MAX;
    super::note_trip();
    trip_entry
}

/// thread_state returns this thread's state, creating it on first use
///
/// The limits come from the system rather than from a runtime field whose offset changes between versions. A thread
/// with an unusually small stack has its reserve capped at a quarter of the stack, and one smaller than the floor is
/// left unguarded rather than turned into a permanent trip.
unsafe fn thread_state(rsp_hint: usize) -> Option<&'static mut ThreadState> {
    let index = tls_index();
    if index == TLS_OUT_OF_INDEXES {
        return None;
    }

    let raw = TlsGetValue(index);
    if !raw.is_null() {
        return Some(&mut *(raw as *mut ThreadState));
    }

    let mut low = 0usize;
    let mut high = 0usize;
    GetCurrentThreadStackLimits(&mut low, &mut high);

    let span = high.saturating_sub(low);
    let state = if span < MIN_GUARDED_STACK {
        super::note_thread_disabled();
        ThreadState { limit: 0, quiet: 0, normal_limit: 0 }
    } else {
        let mut reserve = super::reserve();
        let cap = span / 4;
        if reserve > cap {
            reserve = cap;
        }
        let normal_limit = low + reserve;
        ThreadState { limit: normal_limit, quiet: 0, normal_limit }
    };

    let leaked = Box::into_raw(Box::new(state));
    TlsSetValue(index, leaked as *mut c_void);
    super::note_thread_init();

    //The fast path reads the slot straight out of the TEB, the helper through the API; a disagreement means the layout
    //is not what the emitted code assumes, and the guard is turned off for this thread rather than run blind
    if slot_from_teb(index) != leaked as usize {
        (*leaked).limit = 0;
        super::note_thread_disabled();
        logging::write(&format!(
            "the TEB slot for TLS index {index} does not match TlsGetValue, the guard is off for this thread"
        ));
    }

    if trace() {
        logging::write(&format!(
            "thread state: low {low:#x}, high {high:#x}, entry rsp {rsp_hint:#x}, limit {:#x}, quiet {}",
            (*leaked).limit, (*leaked).quiet
        ));
    }

    Some(&mut *leaked)
}

/// tls_index reserves this layer's TLS slot, once
fn tls_index() -> u32 {
    *TLS_INDEX.get_or_init(|| unsafe {
        let index = TlsAlloc();
        if index == TLS_OUT_OF_INDEXES {
            logging::write("the stack guard could not reserve a TLS slot");
        }
        index
    })
}

/// install rewrites one method entry
pub fn install(function_id: usize, code_start: usize, code_size: u32) {
    let started = Instant::now();

    if code_start == 0 || code_size < 5 || code_start % 8 != 0 {
        super::note_decode_skipped();
        return;
    }

    //The throw entry itself is never hooked: a trampoline that jumps back into the throw would never leave
    if code_start == super::trip_entry() {
        super::note_decode_skipped();
        return;
    }

    if tls_index() == TLS_OUT_OF_INDEXES {
        super::note_alloc_skipped();
        return;
    }

    //The address is the key: a method compiled again reports a new one, and each version is worth its own hook
    {
        let addresses = HOOKED_ADDRESSES.get_or_init(|| Mutex::new(HashSet::new()));
        let Ok(mut addresses) = addresses.lock() else {
            return;
        };
        if !addresses.insert(code_start) {
            super::note_deduplicated();
            return;
        }
    }

    //The decode reads a snapshot, so a patch written by another thread cannot change the bytes underneath it
    let mut snapshot = [0u8; MAX_ENTRY_BYTES];
    let available = (code_size as usize).min(MAX_ENTRY_BYTES);
    unsafe {
        ptr::copy_nonoverlapping(code_start as *const u8, snapshot.as_mut_ptr(), available);
    }

    let Some((stolen, consumed)) = decode_stolen(&snapshot[..available], code_start) else {
        super::note_decode_skipped();
        return;
    };

    let Some(slot) = allocate_slot(code_start) else {
        super::note_alloc_skipped();
        return;
    };

    let check = build_check();
    let prologue_pad = slot + check.bytes.len();
    let prologue_address = prologue_pad + 4;

    let block = InstructionBlock::new(&stolen, prologue_address as u64);
    let Ok(encoded) = BlockEncoder::encode(64, block, BlockEncoderOptions::NONE) else {
        super::note_decode_skipped();
        return;
    };

    let mut image = check.bytes;
    image.extend_from_slice(&[0xF3, 0x0F, 0x1E, 0xFA]);
    image.extend_from_slice(&encoded.code_buffer);

    if image.len() + 200 > SLOT_SIZE {
        super::note_decode_skipped();
        return;
    }

    let resume_disp = image.len() + 1;
    image.push(0xE9);
    image.extend_from_slice(&[0u8; 4]);

    let slow_offset = image.len();
    let slow = build_slow_path();
    image.extend_from_slice(&slow.bytes);

    let slot_offset = align_up(image.len(), 8);
    image.resize(slot_offset + 8, 0);
    if image.len() > SLOT_SIZE {
        super::note_decode_skipped();
        return;
    }
    image[slot_offset..slot_offset + 8]
        .copy_from_slice(&(ncn_stack_slow_entry as *const () as usize).to_le_bytes());

    let slow_address = slot + slow_offset;
    let in_range = write_rel32(&mut image, check.jz_disp, slow_address, slot)
        && write_rel32(&mut image, check.jb_disp, slow_address, slot)
        && write_rel32(&mut image, resume_disp, code_start + consumed, slot)
        && write_rel32(&mut image, slow_offset + slow.prologue_disp, prologue_pad, slot)
        && write_rel32(&mut image, slow_offset + slow.call_disp, slot + slot_offset, slot);

    if !in_range {
        super::note_decode_skipped();
        return;
    }

    unsafe {
        ptr::copy_nonoverlapping(image.as_ptr(), slot as *mut u8, image.len());
        FlushInstructionCache(GetCurrentProcess(), slot as *const c_void, image.len());
    }

    if !patch_entry(code_start, slot, &snapshot[..consumed]) {
        super::note_patch_skipped();
        return;
    }

    super::note_hooked(SLOT_SIZE, started.elapsed().as_nanos() as u64);

    if trace() {
        let sample = &snapshot[..available.min(8)];
        logging::write(&format!("hook {code_start:#x} displacing {consumed} bytes: {sample:02X?}"));
    }

    if !FIRST_HOOK.swap(true, Ordering::SeqCst) {
        logging::write(&format!(
            "the first machine-code stack check was installed, function id {function_id:#x} at {code_start:#x}, \
             {} bytes displaced",
            consumed
        ));
    }
}

/// decode_stolen copies instructions until at least five bytes are covered
///
/// Five bytes are what the entry jump needs. A body whose first instructions run away, end in a return or are not a
/// prologue at all is refused: replaying them would either never come back or change control flow.
fn decode_stolen(bytes: &[u8], address: usize) -> Option<(Vec<Instruction>, usize)> {
    let mut decoder = Decoder::with_ip(64, bytes, address as u64, DecoderOptions::NONE);
    let mut stolen = Vec::with_capacity(MAX_STOLEN);
    let mut consumed = 0usize;

    while consumed < 5 {
        let instruction = decoder.decode();
        if instruction.is_invalid() {
            return None;
        }

        match instruction.flow_control() {
            FlowControl::Return
            | FlowControl::UnconditionalBranch
            | FlowControl::IndirectBranch
            | FlowControl::Exception => return None,
            _ => {}
        }

        consumed += instruction.len();
        stolen.push(instruction);

        if stolen.len() > MAX_STOLEN || consumed > MAX_ENTRY_BYTES {
            return None;
        }
    }

    Some((stolen, consumed))
}

/// CheckPrefix is the position of the branch operands that are patched once the layout is known
struct CheckPrefix {
    bytes: Vec<u8>,
    jz_disp: usize,
    jb_disp: usize,
}

/// build_check emits the fast path
///
/// The expressions spell out what the assembler would: the TLS array hangs off the TEB, the slot holds this thread's
/// state pointer, and the limit is its first field. Every register touched is scratch at a call boundary.
fn build_check() -> CheckPrefix {
    let mut bytes = Vec::with_capacity(40);

    bytes.extend_from_slice(&[0xF3, 0x0F, 0x1E, 0xFA]);

    //Slots below 64 live in the inline TlsSlots array rather than the expansion array the TEB points at, and TlsAlloc
    //hands out the low ones first. The offset is read once here and checked again when a thread state is created.
    let index = tls_index();
    if index < 64 {
        bytes.extend_from_slice(&[0x65, 0x48, 0x8B, 0x04, 0x25]);
        bytes.extend_from_slice(&(0x1480u32 + index * 8).to_le_bytes());
    } else {
        bytes.extend_from_slice(&[0x65, 0x48, 0x8B, 0x04, 0x25]);
        bytes.extend_from_slice(&(TEB_TLS_POINTER as u32).to_le_bytes());
        bytes.extend_from_slice(&[0x48, 0x8B, 0x80]);
        bytes.extend_from_slice(&index.wrapping_mul(8).to_le_bytes());
    }

    bytes.extend_from_slice(&[0x48, 0x85, 0xC0]);
    bytes.extend_from_slice(&[0x0F, 0x84]);
    let jz_disp = bytes.len();
    bytes.extend_from_slice(&[0u8; 4]);

    //The first field of the state is the limit; rax is scratch at a call boundary, so it can carry it away
    bytes.extend_from_slice(&[0x48, 0x8B, 0x00]);

    bytes.extend_from_slice(&[0x48, 0x39, 0xC4]);
    bytes.extend_from_slice(&[0x0F, 0x82]);
    let jb_disp = bytes.len();
    bytes.extend_from_slice(&[0u8; 4]);

    CheckPrefix { bytes, jz_disp, jb_disp }
}

/// SlowPath is the cold half of a trampoline
struct SlowPath {
    bytes: Vec<u8>,
    call_disp: usize,
    prologue_disp: usize,
}

/// build_slow_path emits the register save, the call into this library and the two ways back
///
/// The prologue may have consumed any of the argument registers and the first four vector registers, so all of them are
/// saved before the helper runs and put back before the displaced instructions execute. The helper answers zero for
/// "replay", anything else is an address to jump to with the stack exactly as the caller left it.
fn build_slow_path() -> SlowPath {
    let mut bytes = Vec::with_capacity(128);

    bytes.push(0x9C); // pushfq
    bytes.push(0x51); // push rcx
    bytes.push(0x52); // push rdx
    bytes.extend_from_slice(&[0x41, 0x50]); // push r8
    bytes.extend_from_slice(&[0x41, 0x51]); // push r9
    bytes.extend_from_slice(&[0x41, 0x52]); // push r10
    bytes.extend_from_slice(&[0x41, 0x53]); // push r11
    bytes.extend_from_slice(&[0x48, 0x83, 0xEC, 0x40]);
    bytes.extend_from_slice(&[0x0F, 0x11, 0x04, 0x24]); // movups [rsp], xmm0
    bytes.extend_from_slice(&[0x0F, 0x11, 0x4C, 0x24, 0x10]);
    bytes.extend_from_slice(&[0x0F, 0x11, 0x54, 0x24, 0x20]);
    bytes.extend_from_slice(&[0x0F, 0x11, 0x5C, 0x24, 0x30]);

    //Shadow space for the call, then the entry stack pointer is 0x98 bytes above the current one
    bytes.extend_from_slice(&[0x48, 0x83, 0xEC, 0x20]);
    bytes.extend_from_slice(&[0x48, 0x8D, 0x8C, 0x24, 0x98, 0x00, 0x00, 0x00]); // lea rcx, [rsp+0x98]

    //The helper lives in this library and the trampoline may be far away, so the address travels in the page itself
    bytes.extend_from_slice(&[0xFF, 0x15]);
    let call_disp = bytes.len();
    bytes.extend_from_slice(&[0u8; 4]);

    bytes.extend_from_slice(&[0x48, 0x83, 0xC4, 0x20]);
    bytes.extend_from_slice(&[0x0F, 0x10, 0x04, 0x24]);
    bytes.extend_from_slice(&[0x0F, 0x10, 0x4C, 0x24, 0x10]);
    bytes.extend_from_slice(&[0x0F, 0x10, 0x54, 0x24, 0x20]);
    bytes.extend_from_slice(&[0x0F, 0x10, 0x5C, 0x24, 0x30]);
    bytes.extend_from_slice(&[0x48, 0x83, 0xC4, 0x40]);
    bytes.extend_from_slice(&[0x41, 0x5B]); // pop r11
    bytes.extend_from_slice(&[0x41, 0x5A]); // pop r10
    bytes.extend_from_slice(&[0x41, 0x59]); // pop r9
    bytes.extend_from_slice(&[0x41, 0x58]); // pop r8
    bytes.push(0x5A); // pop rdx
    bytes.push(0x59); // pop rcx
    bytes.push(0x9D); // popfq
    bytes.extend_from_slice(&[0x48, 0x85, 0xC0]); // test rax, rax

    bytes.extend_from_slice(&[0x0F, 0x84]);
    let prologue_disp = bytes.len();
    bytes.extend_from_slice(&[0u8; 4]);

    bytes.extend_from_slice(&[0xFF, 0xE0]); // jmp rax

    SlowPath { bytes, call_disp, prologue_disp }
}

/// write_rel32 patches one branch operand for the final address
fn write_rel32(image: &mut [u8], disp_at: usize, target: usize, base: usize) -> bool {
    let next = base as i64 + disp_at as i64 + 4;
    let Ok(delta) = i32::try_from(target as i64 - next) else {
        return false;
    };
    image[disp_at..disp_at + 4].copy_from_slice(&delta.to_le_bytes());
    true
}

/// patch_entry turns the first bytes of the entry into a jump
///
/// The eight bytes are written in one atomic store so no thread can fetch a half instruction; the three bytes the jump
/// does not use are written back unchanged, which keeps whatever instruction follows intact.
fn patch_entry(entry: usize, trampoline: usize, expected: &[u8]) -> bool {
    let mut current = [0u8; 8];
    unsafe {
        ptr::copy_nonoverlapping(entry as *const u8, current.as_mut_ptr(), 8);
    }

    let compared = expected.len().min(8);
    if current[..compared] != expected[..compared] {
        return false;
    }

    let Ok(delta) = i32::try_from(trampoline as i64 - (entry as i64 + 5)) else {
        return false;
    };

    let mut patched = current;
    patched[0] = 0xE9;
    patched[1..5].copy_from_slice(&delta.to_le_bytes());

    let _guard = match PATCH_LOCK.lock() {
        Ok(guard) => guard,
        Err(_) => return false,
    };

    unsafe {
        let mut previous = 0u32;
        if VirtualProtect(entry as *mut c_void, 8, PAGE_EXECUTE_READWRITE, &mut previous) == 0 {
            return false;
        }

        AtomicU64::from_ptr(entry as *mut u64).store(u64::from_le_bytes(patched), Ordering::SeqCst);

        let mut ignored = 0u32;
        VirtualProtect(entry as *mut c_void, 8, previous, &mut ignored);
        FlushInstructionCache(GetCurrentProcess(), entry as *const c_void, 8);
    }

    true
}

/// allocate_slot carves one slot out of a chunk that sits within reach of the entry
fn allocate_slot(code_start: usize) -> Option<usize> {
    let chunks = CHUNKS.get_or_init(|| Mutex::new(Vec::new()));
    let mut chunks = chunks.lock().ok()?;

    for chunk in chunks.iter_mut() {
        if chunk.used + SLOT_SIZE <= CHUNK_SIZE && is_near(chunk.base, code_start) {
            let at = chunk.base + chunk.used;
            chunk.used += SLOT_SIZE;
            return Some(at);
        }
    }

    let base = allocate_chunk_near(code_start)?;
    chunks.push(Chunk { base, used: SLOT_SIZE });
    Some(base)
}

/// allocate_chunk_near asks for a page inside the two gigabyte window a relative jump can reach
fn allocate_chunk_near(code_start: usize) -> Option<usize> {
    if let Some(allocate) = virtual_alloc2() {
        let lowest = code_start.saturating_sub(NEAR_LIMIT);
        let highest = code_start.saturating_add(NEAR_LIMIT);
        let requirements = MemAddressRequirements { lowest, highest, alignment: 0x10000 };
        let mut parameter = MemExtendedParameter {
            kind: 1, // MemExtendedParameterAddressRequirements
            reserved: 0,
            value: &requirements as *const _ as usize,
        };

        let base = unsafe {
            allocate(
                GetCurrentProcess(),
                ptr::null_mut(),
                CHUNK_SIZE,
                MEM_COMMIT | MEM_RESERVE,
                PAGE_EXECUTE_READWRITE,
                &mut parameter,
                1,
            )
        };

        if !base.is_null() {
            return Some(base as usize);
        }
    }

    //Older systems have no VirtualAlloc2; walk outwards until a free page turns up
    let aligned = code_start & !0xFFFF;
    let mut delta = 0x10_0000usize;
    while delta < NEAR_LIMIT {
        for candidate in [aligned.checked_add(delta), aligned.checked_sub(delta)] {
            let Some(address) = candidate else { continue };
            let base = unsafe {
                VirtualAlloc(
                    address as *mut c_void,
                    CHUNK_SIZE,
                    MEM_COMMIT | MEM_RESERVE,
                    PAGE_EXECUTE_READWRITE,
                )
            };
            if !base.is_null() {
                return Some(base as usize);
            }
        }
        delta += 0x10_0000;
    }

    None
}

/// virtual_alloc2 resolves the exported entry once; the system without it falls back to the scan
fn virtual_alloc2() -> Option<VirtualAlloc2> {
    *VIRTUAL_ALLOC2.get_or_init(|| unsafe {
        let module = GetModuleHandleA(c"kernel32.dll".as_ptr() as *const u8);
        if module.is_null() {
            return None;
        }

        let address = GetProcAddress(module, c"VirtualAlloc2".as_ptr() as *const u8);
        if address.is_null() {
            return None;
        }

        Some(std::mem::transmute::<*mut c_void, VirtualAlloc2>(address))
    })
}

/// slot_from_teb reads the way the trampoline does, without going through the TLS API
///
/// Slots below 64 sit in the inline TlsSlots array at TEB+0x1480; the expansion array the TEB points at only serves the
/// higher indices. Both forms are what the emitted check uses, so this is also the check that the emitted code is right.
unsafe fn slot_from_teb(index: u32) -> usize {
    let teb: usize;
    core::arch::asm!("mov {}, gs:[0x30]", out(reg) teb);
    if teb == 0 {
        return 0;
    }

    if index < 64 {
        return *((teb as *const usize).add(0x1480 / 8 + index as usize));
    }

    let array: usize;
    core::arch::asm!("mov {}, gs:[0x58]", out(reg) array);
    if array == 0 {
        return 0;
    }

    *((array as *const usize).add(index as usize))
}

/// debug_state reports what the trampoline and the helper each see for this thread
///
/// The fast path reads the TLS array through the TEB, while the helper goes through TlsGetValue; a mismatch here means
/// the slot or the offset is wrong, and the symptom is every call taking the slow path.
pub fn debug_state() -> [usize; 7] {
    let index = tls_index();
    let (array, teb) = unsafe {
        let array: usize;
        let teb: usize;
        core::arch::asm!("mov {array}, gs:[0x58]", "mov {teb}, gs:[0x30]", array = out(reg) array, teb = out(reg) teb);
        (array, teb)
    };

    let from_teb = if index != TLS_OUT_OF_INDEXES && array != 0 {
        unsafe { *((array as *const usize).add(index as usize)) }
    } else {
        0
    };

    //TlsSlots is an inline array at TEB+0x1480 on x64; slots below 64 live there rather than in the expansion array
    let from_slots = if index != TLS_OUT_OF_INDEXES && teb != 0 {
        unsafe { *((teb as *const usize).add((0x1480 / 8) + index as usize)) }
    } else {
        0
    };

    let from_api = unsafe { TlsGetValue(index) } as usize;
    let limit = if from_api != 0 {
        unsafe { (*(from_api as *const ThreadState)).limit }
    } else {
        0
    };

    let probe = 0usize;
    let rsp = &probe as *const usize as usize;

    [index as usize, array, from_teb, from_slots, from_api, limit, rsp]
}

/// trace answers whether every installed hook is written to the log
fn trace() -> bool {
    *TRACE.get_or_init(|| {
        std::env::var("NCN_HOOK_TRACE")
            .map(|value| !value.is_empty() && value != "0")
            .unwrap_or(false)
    })
}

/// is_near reports whether a relative jump between the two addresses fits with margin to spare
fn is_near(from: usize, to: usize) -> bool {
    from.abs_diff(to) < NEAR_LIMIT
}

fn align_up(value: usize, alignment: usize) -> usize {
    (value + alignment - 1) & !(alignment - 1)
}

/// addresses copies the hooked entry addresses out, for the harness and for a crash report
pub fn addresses(out: *mut usize, capacity: usize) -> usize {
    if out.is_null() || capacity == 0 {
        return 0;
    }

    let Some(addresses) = HOOKED_ADDRESSES.get() else {
        return 0;
    };
    let Ok(addresses) = addresses.lock() else {
        return 0;
    };

    let count = addresses.len().min(capacity);
    for (index, address) in addresses.iter().take(count).enumerate() {
        unsafe {
            *out.add(index) = *address;
        }
    }
    count
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn decodes_push_frame_with_a_large_sub() {
        let bytes = [0x55, 0x48, 0x81, 0xEC, 0xB0, 0x00, 0x00, 0x00];
        let (stolen, consumed) = decode_stolen(&bytes, 0x1000).expect("prologue");
        assert_eq!(consumed, 8);
        assert_eq!(stolen.len(), 2);
    }

    #[test]
    fn decodes_a_cet_landing_pad() {
        let bytes = [0xF3, 0x0F, 0x1E, 0xFA, 0x55, 0x48, 0x83, 0xEC, 0x20];
        let (stolen, consumed) = decode_stolen(&bytes, 0x1000).expect("prologue");
        //The landing pad and the frame push are exactly the five bytes the jump needs
        assert_eq!(consumed, 5);
        assert_eq!(stolen.len(), 2);
    }

    #[test]
    fn decodes_a_small_frame() {
        let bytes = [0x48, 0x83, 0xEC, 0x28, 0x48, 0x89, 0x4C, 0x24, 0x10];
        let (stolen, consumed) = decode_stolen(&bytes, 0x1000).expect("prologue");
        assert_eq!(consumed, 9);
        assert_eq!(stolen.len(), 2);
    }

    #[test]
    fn refuses_a_body_that_returns_or_jumps() {
        assert!(decode_stolen(&[0xC3, 0x90, 0x90, 0x90, 0x90], 0x1000).is_none());
        assert!(decode_stolen(&[0xE9, 0x00, 0x00, 0x00, 0x00], 0x1000).is_none());
    }

    #[test]
    fn refuses_a_truncated_stream() {
        assert!(decode_stolen(&[0x48, 0x8B], 0x1000).is_none());
    }

    #[test]
    fn relocates_a_rip_relative_operand() {
        //mov rax, [rip+0x100]; the block encoder has to recompute the displacement for the new address
        let bytes = [0x48, 0x8B, 0x05, 0x00, 0x01, 0x00, 0x00];
        let (stolen, consumed) = decode_stolen(&bytes, 0x1_0000_0000).expect("prologue");
        assert_eq!(consumed, 7);

        let block = InstructionBlock::new(&stolen, 0x1_4000_0000);
        let encoded = BlockEncoder::encode(64, block, BlockEncoderOptions::NONE).expect("relocated");
        assert_ne!(&encoded.code_buffer[3..7], &[0x00, 0x01, 0x00, 0x00]);
    }

    #[test]
    fn relative_displacement_is_refused_out_of_range() {
        let mut image = [0u8; 8];
        assert!(!write_rel32(&mut image, 0, 0x1_0000_0000, 0x10));
    }
}
