//! Linux signal side of the native crash path
//!
//! The runtime installs handlers of its own for SIGSEGV: the garbage collector uses write-barrier faults and
//! NullReferenceException detection rides on the same signal. So this has to chain rather than replace. The previous
//! action is kept, and once the report is down it is put back and the signal raised again, which is the same
//! "observe only" contract the Windows vectored handler follows.
//!
//! The frame layout used here is the x86-64 Linux one. Other unixes lay ucontext_t out differently, so this is only
//! correct where it is meant to run.

use crate::crash;
use std::ffi::c_void;
use std::sync::atomic::{AtomicBool, AtomicUsize, Ordering};

/// Signal numbers, as the kernel defines them on Linux
pub const SIGILL: i32 = 4;
pub const SIGABRT: i32 = 6;
pub const SIGBUS: i32 = 7;
pub const SIGFPE: i32 = 8;
pub const SIGSEGV: i32 = 11;

/// The signals worth recording: every one of them means the process is past the point managed code can help
const WATCHED: [i32; 5] = [SIGILL, SIGABRT, SIGBUS, SIGFPE, SIGSEGV];

const SA_SIGINFO: i32 = 4;
const SA_ONSTACK: i32 = 0x0800_0000;
const SA_RESTART: i32 = 0x1000_0000;

/// Enough for the handler to run and format a report after the thread's own stack has been overrun
const ALT_STACK_SIZE: usize = 256 * 1024;

#[repr(C)]
struct SigAction {
    handler: usize,
    mask: [u64; 16],
    flags: i32,
    restorer: usize,
}

/// stack_t: ss_sp, ss_flags (int plus padding), ss_size
#[repr(C)]
struct Stack {
    sp: *mut c_void,
    flags: i32,
    size: usize,
}

extern "C" {
    fn sigaction(signal: i32, action: *const SigAction, old: *mut SigAction) -> i32;
    fn sigaltstack(stack: *const Stack, old: *mut Stack) -> i32;
    fn raise(signal: i32) -> i32;
}

/// The actions that were in place before this handler went in, one slot per watched signal
static PREVIOUS: [AtomicUsize; 5] = [
    AtomicUsize::new(0),
    AtomicUsize::new(0),
    AtomicUsize::new(0),
    AtomicUsize::new(0),
    AtomicUsize::new(0),
];

/// A second fault while the report is being written must not start another report
static REPORTED: AtomicBool = AtomicBool::new(false);

/// The alternate stack has to outlive the handler, so it is allocated once and never freed
static mut ALT_STACK: [u8; ALT_STACK_SIZE] = [0; ALT_STACK_SIZE];

/// install puts the handler in front of the runtime's own, keeping the old action for the chain
pub fn install() -> Result<(), String> {
    unsafe {
        //Without an alternate stack a fault taken while the thread stack is exhausted would re-enter the handler
        let stack = Stack {
            sp: std::ptr::addr_of_mut!(ALT_STACK) as *mut c_void,
            flags: 0,
            size: ALT_STACK_SIZE,
        };
        if sigaltstack(&stack, std::ptr::null_mut()) != 0 {
            return Err("sigaltstack was refused".to_string());
        }
    }

    let action = SigAction {
        handler: handle as usize,
        mask: [0; 16],
        //SA_ONSTACK is what makes the alternate stack reachable; SA_RESTART keeps unrelated syscalls retrying
        flags: SA_SIGINFO | SA_ONSTACK | SA_RESTART,
        restorer: 0,
    };

    for (slot, signal) in WATCHED.iter().enumerate() {
        let mut previous = SigAction {
            handler: 0,
            mask: [0; 16],
            flags: 0,
            restorer: 0,
        };

        let result = unsafe { sigaction(*signal, &action, &mut previous) };
        if result != 0 {
            return Err(format!("sigaction was refused for signal {signal}"));
        }

        PREVIOUS[slot].store(previous.handler, Ordering::SeqCst);
    }

    Ok(())
}

/// uninstall restores the actions that were in place before, used while the runtime is shutting down
pub fn uninstall() {
    for (slot, signal) in WATCHED.iter().enumerate() {
        let handler = PREVIOUS[slot].load(Ordering::SeqCst);
        if handler == 0 {
            continue;
        }

        //The mask and flags of the original action are not kept; restoring the handler is what matters here
        let action = SigAction {
            handler,
            mask: [0; 16],
            flags: SA_SIGINFO | SA_RESTART,
            restorer: 0,
        };
        unsafe { sigaction(*signal, &action, std::ptr::null_mut()) };
    }
}

/// handle records the fault and then hands the signal on to whatever was installed before
unsafe extern "C" fn handle(signal: i32, info: *mut c_void, context: *mut c_void) {
    if !REPORTED.swap(true, Ordering::SeqCst) {
        let address = if info.is_null() {
            0
        } else {
            (*(info as *const crash::unix::SigInfo)).fault_address
        };
        crash::handle_signal(signal as u32, address, context);
    }

    //Put the previous action back and raise the signal again: the runtime still has to be the one that ends this
    if let Some(slot) = WATCHED.iter().position(|candidate| *candidate == signal) {
        let handler = PREVIOUS[slot].load(Ordering::SeqCst);
        if handler != 0 {
            let action = SigAction {
                handler,
                mask: [0; 16],
                flags: SA_SIGINFO | SA_RESTART,
                restorer: 0,
            };
            sigaction(signal, &action, std::ptr::null_mut());
            raise(signal);
        }
    }
}
