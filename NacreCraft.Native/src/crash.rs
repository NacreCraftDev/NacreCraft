//! Native crash component for the exceptions managed code can no longer handle
//!
//! .NET treats a hardware fault as a corrupted state: it ends the process without dispatching a managed event, so the
//! managed crash handler never runs and no ordinary report exists for that run. This component records the fault from
//! the layer that is still standing - the vectored exception path on Windows, a signal handler on Linux - prints a
//! short banner and writes a report beside the ordinary ones.
//!
//! It only observes. On both platforms control is handed back unchanged so the runtime can end the process its own way;
//! taking the fault over would mean continuing with a thread whose state is no longer trustworthy.

use crate::report;
use std::ffi::c_void;

/// EXCEPTION_CONTINUE_SEARCH, and on the signal side the value that lets the old handler run
pub const CONTINUE_SEARCH: i32 = 0;

/// STD_OUTPUT_HANDLE / STD_ERROR_HANDLE
#[cfg(windows)]
const STD_OUTPUT_HANDLE: u32 = 0xFFFF_FFF5u32 as u32;
#[cfg(windows)]
const STD_ERROR_HANDLE: u32 = 0xFFFF_FFF4u32 as u32;

/// How many machine words of raw stack are recorded around the faulting frame
const STACK_WORDS: usize = 8;

/// Registers is the subset both platforms can supply
/// Every x86-64 fault path carries these, so one report body serves both
#[derive(Clone, Copy, Default)]
pub struct Registers {
    pub rip: u64,
    pub rsp: u64,
    pub rbp: u64,
    pub eflags: u64,
    pub rax: u64,
    pub rbx: u64,
    pub rcx: u64,
    pub rdx: u64,
    pub rsi: u64,
    pub rdi: u64,
    pub r8: u64,
    pub r9: u64,
    pub r10: u64,
    pub r11: u64,
    pub r12: u64,
    pub r13: u64,
    pub r14: u64,
    pub r15: u64,
}

/// Fault is everything the platform layer managed to read before the process goes down
pub struct Fault {
    /// Platform fault code: an NTSTATUS on Windows, a signal number on Linux
    pub code: u32,
    pub name: &'static str,
    pub address: u64,
    pub thread_id: u32,
    /// What the fault was doing, when the platform can tell
    pub access: Option<String>,
    pub registers: Option<Registers>,
    pub stack: Vec<u64>,
    /// Why the stack sample is short or missing
    pub stack_note: String,
    /// Which module the faulting address belongs to, when it could be resolved
    pub module: String,
}

/// name_of gives the faults worth naming; anything else is still reported by its code alone
/// The code space differs per platform, so the table does too
#[cfg(windows)]
fn name_of(code: u32) -> Option<&'static str> {
    match code {
        0xC000_0005 => Some("EXCEPTION_ACCESS_VIOLATION"),
        0xC000_0006 => Some("EXCEPTION_IN_PAGE_ERROR"),
        0xC000_001D => Some("EXCEPTION_ILLEGAL_INSTRUCTION"),
        0xC000_0025 => Some("EXCEPTION_DATATYPE_MISALIGNMENT"),
        0xC000_008C => Some("EXCEPTION_ARRAY_BOUNDS_EXCEEDED"),
        0xC000_0094 => Some("EXCEPTION_INT_DIVIDE_BY_ZERO"),
        0xC000_0095 => Some("EXCEPTION_INT_OVERFLOW"),
        0xC000_0096 => Some("EXCEPTION_PRIV_INSTRUCTION"),
        0xC000_0409 => Some("STATUS_STACK_BUFFER_OVERRUN"),
        _ => None,
    }
}

/// SIGSEGV / SIGBUS / SIGILL / SIGFPE / SIGABRT
#[cfg(unix)]
fn name_of(code: u32) -> Option<&'static str> {
    match code {
        4 => Some("SIGILL"),
        6 => Some("SIGABRT"),
        7 => Some("SIGBUS"),
        8 => Some("SIGFPE"),
        11 => Some("SIGSEGV"),
        _ => None,
    }
}

/// handles decides whether a fault belongs to this component
/// Stack overflow is deliberately absent: that path has to run on the stack Windows keeps in reserve, so it is served by
/// its own handler instead of this one, which formats more than fits there
pub fn handles(code: u32) -> bool {
    name_of(code).is_some()
}

/// compose renders the report and the console banner from one fault
/// Formatting lives here rather than in the platform layer so both sides produce the same text
pub fn compose(fault: &Fault, stamp: u64, path: &std::path::Path) -> (String, String) {
    let description = fault.name;
    let code = fault.code;
    let address = fault.address;

    let mut head = String::new();
    head.push_str("---- NacreCraft Native Crash Report ----\n");
    head.push_str(&format!("Time (unix): {stamp}\n"));
    head.push_str(&format!(
        "Description: {description} ended this process before managed code could report it\n\n"
    ));
    head.push_str(
        "The fault was a hardware exception, which the runtime treats as an unrecoverable state: it ends the process\n\
         without dispatching a managed event, so no ordinary crash report exists for this run. What follows is what the\n\
         native layer was able to read from the fault.\n\n",
    );

    head.push_str(&format!("-- {description} --\n"));
    head.push_str(&format!("Code: 0x{code:08X}\n"));
    head.push_str(&format!("Faulting address: 0x{address:016X}\n"));
    head.push_str(&format!("Module: {}\n", fault.module));
    head.push_str(&format!("Thread id: {}\n", fault.thread_id));
    if let Some(note) = &fault.access {
        head.push_str(&format!("Access: {note}\n"));
    }

    head.push_str("\n-- Registers --\n");
    match fault.registers {
        Some(state) => {
            head.push_str(&format!(
                "RIP 0x{:016X}  RSP 0x{:016X}  RBP 0x{:016X}  EFLAGS 0x{:08X}\n",
                state.rip, state.rsp, state.rbp, state.eflags
            ));
            head.push_str(&format!(
                "RAX 0x{:016X}  RBX 0x{:016X}  RCX 0x{:016X}  RDX 0x{:016X}\n",
                state.rax, state.rbx, state.rcx, state.rdx
            ));
            head.push_str(&format!(
                "RSI 0x{:016X}  RDI 0x{:016X}  R8  0x{:016X}  R9  0x{:016X}\n",
                state.rsi, state.rdi, state.r8, state.r9
            ));
            head.push_str(&format!(
                "R10 0x{:016X}  R11 0x{:016X}  R12 0x{:016X}  R13 0x{:016X}\n",
                state.r10, state.r11, state.r12, state.r13
            ));
            head.push_str(&format!(
                "R14 0x{:016X}  R15 0x{:016X}\n",
                state.r14, state.r15
            ));
        }
        None => head.push_str("no register context was provided\n"),
    }

    head.push_str("\n-- Stack (raw words at RSP) --\n");
    if fault.stack.is_empty() {
        head.push_str(&format!("{}\n", fault.stack_note));
    } else {
        for (index, word) in fault.stack.iter().enumerate() {
            // The first word is the return address of the faulting frame, which is the frame worth looking at
            let note = if index == 0 { "  <- likely return address" } else { "" };
            head.push_str(&format!("  [RSP+{:02X}] 0x{:016X}{note}\n", index * 8, word));
        }
        if !fault.stack_note.is_empty() {
            head.push_str(&format!("  {}\n", fault.stack_note));
        }
    }

    head.push_str(
        "\nSymbol names are left out on purpose: resolving them loads a library and can deadlock on the state this\n\
         thread is in. Run the process under a debugger or enable native dumps for a symbolised stack.\n",
    );

    let banner = format!(
        "\n################################################################\n\
         [NacreCraft] The process is going down.\n\
         \n\
         At {stamp} it faulted with {description} (0x{code:08X}){access_suffix}\n\
         Location: 0x{address:016X} in {module}\n\
         \n\
         Recent stack:\n{stack_lines}\
         \n\
         Report: {path}\n\
         The runtime will now end the process. This fault cannot be recovered from.\n\
         ################################################################\n",
        access_suffix = fault
            .access
            .as_ref()
            .map(|note| format!(", {note}"))
            .unwrap_or_default(),
        module = fault.module,
        stack_lines = if fault.stack.is_empty() {
            format!("  ({})\n", fault.stack_note)
        } else {
            fault
                .stack
                .iter()
                .enumerate()
                .map(|(index, word)| format!("  [RSP+{:02X}] 0x{:016X}\n", index * 8, word))
                .collect::<String>()
        },
        path = path.display()
    );

    (head, banner)
}

/// record writes the report, prints the banner and returns the code the caller should pass on
/// Unsafe because the console write goes through the platform's raw output call
pub unsafe fn record(fault: &Fault) -> i32 {
    let stamp = report::unix_stamp();
    let path = report::native_crash_path(stamp);
    let (text, banner) = compose(fault, stamp, &path);

    report::write_native_crash(&path, &text);
    write_console(&banner);
    CONTINUE_SEARCH
}

/// write_console mirrors the banner to the console without going through the standard library
/// The runtime's own output locks can be held by the thread that just faulted, and a crash report is not worth
/// deadlocking over
#[cfg(windows)]
unsafe fn write_console(text: &str) {
    use win::{GetStdHandle, WriteFile};

    for kind in [STD_ERROR_HANDLE, STD_OUTPUT_HANDLE] {
        let handle = GetStdHandle(kind);
        if handle.is_null() || handle as isize == -1 {
            continue;
        }
        let mut written: u32 = 0;
        let _ = WriteFile(
            handle,
            text.as_ptr(),
            text.len() as u32,
            &mut written,
            std::ptr::null_mut(),
        );
    }
}

/// write uses the raw syscall wrapper: it is async-signal-safe, which is the only thing that matters here
#[cfg(unix)]
unsafe fn write_console(text: &str) {
    for descriptor in [2i32, 1i32] {
        let _ = libc_write(descriptor, text.as_ptr(), text.len());
    }
}

#[cfg(unix)]
extern "C" {
    #[link_name = "write"]
    fn libc_write(descriptor: i32, buffer: *const u8, length: usize) -> isize;
}

//Everything the two platforms do not share lives behind these modules, so the report body above stays identical.
//Windows reaches this through the vectored exception handler in stack.rs; Linux through signals.rs.

/// win holds the Windows-only pieces: the CONTEXT layout, the memory map query and the raw reads
#[cfg(windows)]
pub mod win {
    use super::{Fault, Registers, STACK_WORDS};
    use std::ffi::c_void;

    #[repr(C)]
    pub struct Context {
        pub p1_home: u64,
        pub p2_home: u64,
        pub p3_home: u64,
        pub p4_home: u64,
        pub p5_home: u64,
        pub p6_home: u64,
        pub context_flags: u32,
        pub mx_csr: u32,
        pub seg_cs: u16,
        pub seg_ds: u16,
        pub seg_es: u16,
        pub seg_fs: u16,
        pub seg_gs: u16,
        pub seg_ss: u16,
        pub eflags: u32,
        pub dr0: u64,
        pub dr1: u64,
        pub dr2: u64,
        pub dr3: u64,
        pub dr6: u64,
        pub dr7: u64,
        pub rax: u64,
        pub rcx: u64,
        pub rdx: u64,
        pub rbx: u64,
        pub rsp: u64,
        pub rbp: u64,
        pub rsi: u64,
        pub rdi: u64,
        pub r8: u64,
        pub r9: u64,
        pub r10: u64,
        pub r11: u64,
        pub r12: u64,
        pub r13: u64,
        pub r14: u64,
        pub r15: u64,
        pub rip: u64,
    }

    #[repr(C)]
    struct MemoryBasicInformation {
        base_address: *mut c_void,
        allocation_base: *mut c_void,
        allocation_protect: u32,
        partition_id: u16,
        region_size: usize,
        state: u32,
        protect: u32,
        region_type: u32,
    }

    extern "system" {
        pub fn GetCurrentThreadId() -> u32;
        fn GetCurrentProcess() -> *mut c_void;
        pub fn GetStdHandle(kind: u32) -> *mut c_void;
        pub fn WriteFile(
            handle: *mut c_void,
            buffer: *const u8,
            length: u32,
            written: *mut u32,
            overlapped: *mut c_void,
        ) -> i32;
        fn ReadProcessMemory(
            process: *mut c_void,
            address: *const c_void,
            buffer: *mut c_void,
            size: usize,
            read: *mut usize,
        ) -> i32;
        fn VirtualQuery(address: *const c_void, buffer: *mut MemoryBasicInformation, length: usize) -> usize;
        fn GetModuleFileNameW(module: *mut c_void, buffer: *mut u16, size: u32) -> u32;
    }

    /// access_note turns EXCEPTION_ACCESS_VIOLATION's two parameters into something readable
    pub fn access_note(code: u32, information: &[usize; 15]) -> Option<String> {
        if code != 0xC000_0005 && code != 0xC000_0006 {
            return None;
        }
        let operation = match information[0] {
            0 => "reading",
            1 => "writing",
            8 => "executing",
            _ => "touching",
        };
        Some(format!("{operation} 0x{:016X}", information[1]))
    }

    /// module_of names the module a code address falls into, using the allocation base the memory map reports
    unsafe fn module_of(address: usize) -> String {
        let mut info: MemoryBasicInformation = std::mem::zeroed();
        if VirtualQuery(address as *const c_void, &mut info, std::mem::size_of::<MemoryBasicInformation>()) == 0
            || info.allocation_base.is_null()
        {
            return "unknown".to_string();
        }

        let mut name = [0u16; 260];
        let length = GetModuleFileNameW(info.allocation_base, name.as_mut_ptr(), name.len() as u32);
        if length == 0 {
            return format!("0x{:016X}", info.allocation_base as usize);
        }

        let stem = String::from_utf16_lossy(&name[..length as usize]);
        format!("{stem} +0x{:X}", address - info.allocation_base as usize)
    }

    /// read_stack copies a few words around the faulting frame
    /// ReadProcessMemory is used rather than a direct read: it reports a bad address instead of faulting again
    unsafe fn read_stack(top: usize) -> (Vec<u64>, String) {
        let process = GetCurrentProcess();
        let mut words = Vec::with_capacity(STACK_WORDS);

        for index in 0..STACK_WORDS {
            let address = top + index * std::mem::size_of::<u64>();
            let mut word: u64 = 0;
            let mut read: usize = 0;
            let ok = ReadProcessMemory(
                process,
                address as *const c_void,
                &mut word as *mut u64 as *mut c_void,
                std::mem::size_of::<u64>(),
                &mut read,
            );
            if ok == 0 || read != std::mem::size_of::<u64>() {
                return (words, format!("the stack stopped being readable at 0x{address:016X}"));
            }
            words.push(word);
        }

        (words, String::new())
    }

    /// fault_from builds the shared fault record out of an exception record and its context
    pub unsafe fn fault_from(
        code: u32,
        address: u64,
        information: &[usize; 15],
        context: *mut c_void,
    ) -> Fault {
        let registers = if context.is_null() {
            None
        } else {
            let state = &*(context as *const Context);
            Some(Registers {
                rip: state.rip,
                rsp: state.rsp,
                rbp: state.rbp,
                eflags: state.eflags as u64,
                rax: state.rax,
                rbx: state.rbx,
                rcx: state.rcx,
                rdx: state.rdx,
                rsi: state.rsi,
                rdi: state.rdi,
                r8: state.r8,
                r9: state.r9,
                r10: state.r10,
                r11: state.r11,
                r12: state.r12,
                r13: state.r13,
                r14: state.r14,
                r15: state.r15,
            })
        };

        let (stack, stack_note) = match registers {
            Some(state) => read_stack(state.rsp as usize),
            None => (Vec::new(), "no register context was provided".to_string()),
        };

        Fault {
            code,
            name: super::name_of(code).unwrap_or("an unnamed hardware exception"),
            address,
            thread_id: GetCurrentThreadId(),
            access: access_note(code, information),
            registers,
            stack,
            stack_note,
            module: module_of(address as usize),
        }
    }
}

/// unix holds the Linux pieces: the signal frame layout and the raw reads
#[cfg(unix)]
pub mod unix {
    use super::{Fault, Registers, STACK_WORDS};
    use std::ffi::c_void;

    /// The x86-64 register order the kernel writes into gregs
    const REG_RBP: usize = 10;
    const REG_RBX: usize = 11;
    const REG_RDX: usize = 12;
    const REG_RAX: usize = 13;
    const REG_RCX: usize = 14;
    const REG_RSP: usize = 15;
    const REG_RIP: usize = 16;
    const REG_EFL: usize = 17;
    const REG_RDI: usize = 8;
    const REG_RSI: usize = 9;

    #[repr(C)]
    pub struct UContext {
        pub uc_flags: u64,
        pub uc_link: *mut UContext,
        /// stack_t: ss_sp, ss_flags (int + padding), ss_size
        pub uc_stack: [u64; 3],
        pub uc_mcontext: MContext,
    }

    #[repr(C)]
    pub struct MContext {
        /// REG_R8..REG_R15 first, then RDI..R15's lower half, matching the kernel's gregset order
        pub gregs: [u64; 23],
        pub fpregs: *mut c_void,
        pub reserved: [u64; 8],
    }

    impl UContext {
        /// register reads one entry out of the kernel's gregset
        fn register(&self, index: usize) -> u64 {
            self.uc_mcontext.gregs[index]
        }

        /// registers snapshots the subset the report shows
        pub fn registers(&self) -> Registers {
            Registers {
                rip: self.register(REG_RIP),
                rsp: self.register(REG_RSP),
                rbp: self.register(REG_RBP),
                eflags: self.register(REG_EFL),
                rax: self.register(REG_RAX),
                rbx: self.register(REG_RBX),
                rcx: self.register(REG_RCX),
                rdx: self.register(REG_RDX),
                rsi: self.register(REG_RSI),
                rdi: self.register(REG_RDI),
                r8: self.register(0),
                r9: self.register(1),
                r10: self.register(2),
                r11: self.register(3),
                r12: self.register(4),
                r13: self.register(5),
                r14: self.register(6),
                r15: self.register(7),
            }
        }
    }

    /// siginfo_t, of which only the first fields matter here
    #[repr(C)]
    pub struct SigInfo {
        pub signo: i32,
        pub errno: i32,
        pub code: i32,
        pub padding: i32,
        /// The fault address sits at the start of the sigfault union member
        pub fault_address: u64,
    }

    /// read_stack copies a few words around the faulting frame
    /// The memory is probed through mincore before each read: a direct read of an unmapped page would fault again,
    /// and the second fault would arrive with the handler still running
    unsafe fn read_stack(top: usize) -> (Vec<u64>, String) {
        let mut words = Vec::with_capacity(STACK_WORDS);

        for index in 0..STACK_WORDS {
            let address = top + index * std::mem::size_of::<u64>();
            if !readable(address) {
                return (words, format!("the stack stopped being readable at 0x{address:016X}"));
            }
            words.push(*(address as *const u64));
        }

        (words, String::new())
    }

    /// readable asks mincore whether the page holding the address is mapped
    unsafe fn readable(address: usize) -> bool {
        let page = address & !0xFFF;
        let mut resident: u8 = 0;
        mincore(page as *const c_void, 0x1000, &mut resident as *mut u8 as *mut c_void) == 0
    }

    extern "C" {
        fn mincore(address: *const c_void, length: usize, resident: *mut c_void) -> i32;
        fn syscall(number: i64) -> i64;
    }

    /// SYS_gettid
    const SYS_GETTID: i64 = 186;

    /// thread_id is the kernel thread id, which is what a report reader can match against ps output
    pub fn thread_id() -> u32 {
        unsafe { syscall(SYS_GETTID) as u32 }
    }

    /// fault_from builds the shared fault record out of a signal and its context
    pub unsafe fn fault_from(code: u32, address: u64, context: *mut c_void) -> Fault {
        let registers = if context.is_null() {
            None
        } else {
            Some((&*(context as *const UContext)).registers())
        };

        let (stack, stack_note) = match registers {
            Some(state) => read_stack(state.rsp as usize),
            None => (Vec::new(), "no register context was provided".to_string()),
        };

        Fault {
            code,
            name: super::name_of(code).unwrap_or("an unnamed signal"),
            address,
            thread_id: thread_id(),
            // Linux does not report whether the access was a read or a write, only that the mapping was wrong
            access: None,
            registers,
            stack,
            stack_note,
            // Resolving this needs dladdr, which is not async-signal-safe; the mapping is in /proc/<pid>/maps
            module: "unknown (see /proc/<pid>/maps)".to_string(),
        }
    }
}

/// Entry used by the Windows vectored handler
#[cfg(windows)]
pub unsafe fn handle(exception: *mut crate::stack::ExceptionRecord, context: *mut c_void) -> i32 {
    let fault = win::fault_from(
        (*exception).code,
        (*exception).address as u64,
        &(*exception).information,
        context,
    );
    record(&fault)
}

/// Entry used by the Linux signal handler
#[cfg(unix)]
pub unsafe fn handle_signal(code: u32, address: u64, context: *mut c_void) -> i32 {
    let fault = unix::fault_from(code, address, context);
    record(&fault)
}
