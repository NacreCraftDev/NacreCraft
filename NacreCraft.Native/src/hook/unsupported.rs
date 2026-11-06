//! Placeholder for platforms without the machine-code hook
//!
//! The trampoline is x86-64 and its thread state is read through the Windows TEB, so the other targets compile the same
//! surface with nothing behind it. The IL-injected guard stays in charge there.

/// install does nothing on a platform without the hook
pub fn install(_function_id: usize, _code_start: usize, _code_size: u32) {}

/// addresses has nothing to report on a platform without the hook
pub fn addresses(_out: *mut usize, _capacity: usize) -> usize {
    0
}

/// debug_state has nothing to report on a platform without the hook
pub fn debug_state() -> [usize; 7] {
    [0; 7]
}
