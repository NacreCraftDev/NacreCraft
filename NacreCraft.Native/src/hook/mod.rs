//! Machine-code entry hooks for the stack guard
//!
//! The guard used to be an injected IL call, which was paid once per call and grew every body it touched. This module
//! moves the check to the machine-code entry: JITCompilationFinished reports the address after the body is compiled and
//! the first bytes of the entry become a jump to a trampoline nearby. The trampoline compares the stack pointer against
//! a per-thread soft limit and either replays the instructions it displaced or hands the thread to a managed method that
//! throws. A method the JIT inlined has no entry of its own, so it is never checked; a frame allocation is checked once.
//!
//! Every decision here is made on the runtime's JIT threads, so the work has to stay small and must never unwind.

use std::sync::atomic::{AtomicBool, AtomicU64, AtomicUsize, Ordering};
use std::sync::OnceLock;

#[cfg(all(target_os = "windows", target_arch = "x86_64"))]
mod x64;

#[cfg(all(target_os = "windows", target_arch = "x86_64"))]
use x64 as platform;

#[cfg(not(all(target_os = "windows", target_arch = "x86_64")))]
mod unsupported;

#[cfg(not(all(target_os = "windows", target_arch = "x86_64")))]
use unsupported as platform;

/// Default number of bytes kept between the soft limit and the guard page
///
/// The reserve has to carry the throw, the unwind and the catch that handles it; the value can be moved with
/// NCN_STACK_RESERVE when a crash report shows a run needs more room
pub const DEFAULT_RESERVE: usize = 512 * 1024;

/// The managed entry that throws when the soft limit is crossed
static TRIP_ENTRY: AtomicUsize = AtomicUsize::new(0);

/// Set once the managed side has handed the throw entry over
static READY: AtomicBool = AtomicBool::new(false);

/// Bytes kept between the soft limit and the guard page
static RESERVE: AtomicUsize = AtomicUsize::new(DEFAULT_RESERVE);

/// Whether the machine-code guard was asked for; read once, the variable never changes during a run
static ENABLED: OnceLock<bool> = OnceLock::new();

/// Counters the shell and the managed test harness read back
#[repr(C)]
#[derive(Clone, Copy, Default)]
pub struct HookStats {
    pub attempts: u64,
    pub hooked: u64,
    pub deduplicated: u64,
    pub not_ready: u64,
    pub skipped_decode: u64,
    pub skipped_alloc: u64,
    pub skipped_patch: u64,
    pub trampoline_bytes: u64,
    pub install_nanos: u64,
    pub max_install_nanos: u64,
    pub slow_calls: u64,
    pub trips: u64,
    pub thread_inits: u64,
    pub disabled_threads: u64,
}

//One counter per row of HookStats, kept in the same order so the export below is a straight copy
static ATTEMPTS: AtomicU64 = AtomicU64::new(0);
static HOOKED: AtomicU64 = AtomicU64::new(0);
static DEDUPLICATED: AtomicU64 = AtomicU64::new(0);
static NOT_READY: AtomicU64 = AtomicU64::new(0);
static SKIPPED_DECODE: AtomicU64 = AtomicU64::new(0);
static SKIPPED_ALLOC: AtomicU64 = AtomicU64::new(0);
static SKIPPED_PATCH: AtomicU64 = AtomicU64::new(0);
static TRAMPOLINE_BYTES: AtomicU64 = AtomicU64::new(0);
static INSTALL_NANOS: AtomicU64 = AtomicU64::new(0);
static MAX_INSTALL_NANOS: AtomicU64 = AtomicU64::new(0);
static SLOW_CALLS: AtomicU64 = AtomicU64::new(0);
static TRIPS: AtomicU64 = AtomicU64::new(0);
static THREAD_INITS: AtomicU64 = AtomicU64::new(0);
static DISABLED_THREADS: AtomicU64 = AtomicU64::new(0);

/// is_enabled answers whether the machine-code guard is on
///
/// It is on by default now that the injected path is retired. NCN_STACK_CHECK=0 turns it off for a comparison run, and
/// NCN_STACK_HOOK=0 / NCN_STACK_GUARD=off do the same; the truthy spellings are kept so older scripts keep working.
pub fn is_enabled() -> bool {
    *ENABLED.get_or_init(|| {
        let disabled = |name: &str| {
            std::env::var(name)
                .map(|value| {
                    value == "0"
                        || value.eq_ignore_ascii_case("false")
                        || value.eq_ignore_ascii_case("off")
                })
                .unwrap_or(false)
        };

        let enabled = !(disabled("NCN_STACK_CHECK")
            || disabled("NCN_STACK_HOOK")
            || std::env::var("NCN_STACK_GUARD")
                .map(|value| value.eq_ignore_ascii_case("off"))
                .unwrap_or(false));

        crate::logging::write(if enabled {
            "the machine-code stack guard is on"
        } else {
            "the machine-code stack guard is off for this run"
        });
        enabled
    })
}

/// is_ready answers whether the managed throw entry has been handed over
pub fn is_ready() -> bool {
    READY.load(Ordering::SeqCst) && TRIP_ENTRY.load(Ordering::SeqCst) != 0
}

/// configure takes the managed throw entry and the reserve size
///
/// A zero entry disarms the guard again. The managed side calls it that way before it asks the runtime for the entry
/// address, because finding that address compiles the method and a hook installed on it would lead back into itself.
pub fn configure(trip_entry: usize, reserve: usize) -> i32 {
    if trip_entry == 0 {
        READY.store(false, Ordering::SeqCst);
        TRIP_ENTRY.store(0, Ordering::SeqCst);
        crate::logging::write("the machine-code stack guard was disarmed");
        return 0;
    }

    let reserve = resolve_reserve(reserve);
    RESERVE.store(reserve, Ordering::SeqCst);
    TRIP_ENTRY.store(trip_entry, Ordering::SeqCst);
    READY.store(true, Ordering::SeqCst);

    crate::logging::write(&format!(
        "the machine-code stack guard is armed, trip entry {trip_entry:#x}, reserve {} KB",
        reserve / 1024
    ));
    0
}

/// resolve_reserve prefers the value the managed side passed, then NCN_STACK_RESERVE, then the default
fn resolve_reserve(requested: usize) -> usize {
    if requested != 0 {
        return requested;
    }

    std::env::var("NCN_STACK_RESERVE")
        .ok()
        .and_then(|value| value.trim().parse::<usize>().ok())
        .filter(|value| *value >= 32 * 1024)
        .unwrap_or(DEFAULT_RESERVE)
}

/// trip_entry is the managed method the slow path jumps to
pub(crate) fn trip_entry() -> usize {
    TRIP_ENTRY.load(Ordering::SeqCst)
}

/// reserve is the number of bytes kept above the guard page
pub(crate) fn reserve() -> usize {
    RESERVE.load(Ordering::SeqCst)
}

/// install is called from the shell once a method body has been compiled
///
/// The address is the native code start; a method that was compiled more than once (tier0, tier1, OSR) reports a
/// different address each time and is hooked at each one, so an older version staying in use cannot slip past.
pub fn install(function_id: usize, code_start: usize, code_size: u32) {
    if !is_enabled() {
        return;
    }

    ATTEMPTS.fetch_add(1, Ordering::Relaxed);

    if !is_ready() {
        NOT_READY.fetch_add(1, Ordering::Relaxed);
        return;
    }

    platform::install(function_id, code_start, code_size);
}

/// stats copies the counters out for the shell or the harness
pub fn stats() -> HookStats {
    HookStats {
        attempts: ATTEMPTS.load(Ordering::Relaxed),
        hooked: HOOKED.load(Ordering::Relaxed),
        deduplicated: DEDUPLICATED.load(Ordering::Relaxed),
        not_ready: NOT_READY.load(Ordering::Relaxed),
        skipped_decode: SKIPPED_DECODE.load(Ordering::Relaxed),
        skipped_alloc: SKIPPED_ALLOC.load(Ordering::Relaxed),
        skipped_patch: SKIPPED_PATCH.load(Ordering::Relaxed),
        trampoline_bytes: TRAMPOLINE_BYTES.load(Ordering::Relaxed),
        install_nanos: INSTALL_NANOS.load(Ordering::Relaxed),
        max_install_nanos: MAX_INSTALL_NANOS.load(Ordering::Relaxed),
        slow_calls: SLOW_CALLS.load(Ordering::Relaxed),
        trips: TRIPS.load(Ordering::Relaxed),
        thread_inits: THREAD_INITS.load(Ordering::Relaxed),
        disabled_threads: DISABLED_THREADS.load(Ordering::Relaxed),
    }
}

/// note_hooked records one entry that was rewritten
pub(crate) fn note_hooked(trampoline_bytes: usize, elapsed_nanos: u64) {
    let count = HOOKED.fetch_add(1, Ordering::Relaxed) + 1;
    TRAMPOLINE_BYTES.fetch_add(trampoline_bytes as u64, Ordering::Relaxed);
    INSTALL_NANOS.fetch_add(elapsed_nanos, Ordering::Relaxed);
    MAX_INSTALL_NANOS.fetch_max(elapsed_nanos, Ordering::Relaxed);

    //One line per few hundred keeps the count visible in a run that is stopped before the shutdown summary
    if count % 256 == 0 {
        crate::logging::write(&format!("{count} method entries hooked so far"));
    }
}

pub(crate) fn note_deduplicated() {
    DEDUPLICATED.fetch_add(1, Ordering::Relaxed);
}

pub(crate) fn note_decode_skipped() {
    SKIPPED_DECODE.fetch_add(1, Ordering::Relaxed);
}

pub(crate) fn note_alloc_skipped() {
    SKIPPED_ALLOC.fetch_add(1, Ordering::Relaxed);
}

pub(crate) fn note_patch_skipped() {
    SKIPPED_PATCH.fetch_add(1, Ordering::Relaxed);
}

pub(crate) fn note_slow_call() {
    SLOW_CALLS.fetch_add(1, Ordering::Relaxed);
}

pub(crate) fn note_trip() {
    TRIPS.fetch_add(1, Ordering::Relaxed);
}

pub(crate) fn note_thread_init() {
    THREAD_INITS.fetch_add(1, Ordering::Relaxed);
}

pub(crate) fn note_thread_disabled() {
    DISABLED_THREADS.fetch_add(1, Ordering::Relaxed);
}

/// addresses copies the hooked entry addresses out
pub fn addresses(out: *mut usize, capacity: usize) -> usize {
    platform::addresses(out, capacity)
}

/// debug_state reports the TLS view of the running thread
pub fn debug_state() -> [usize; 7] {
    platform::debug_state()
}
