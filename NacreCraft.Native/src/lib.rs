//! NacreCraft native layer
//!
//! The CLR loads this library as a profiler when the CORECLR_* environment variables are set, which happens before any
//! managed code in the process runs. Nothing injects it: the managed side writes the variables and starts the process
//! again, and the runtime picks the library up from there.
//!
//! Modules:
//! - `crash`: the component that records a hardware fault managed code cannot survive
//! - `il`: IL parsing and encoding, used by the rewrite engine
//! - `logging`: the single diagnostic file every part of the layer writes to
//! - `rejit`: the runtime rewrite engine, driven by the managed side
//! - `report`: the report produced once managed code can no longer run
//! - `stack`: the vectored exception handler that catches a stack overflow
//! - `system`: the startup fingerprint carried into a native crash report

mod crash;
mod hook;
mod icon;
mod il;
mod logging;
mod rejit;
mod report;
#[cfg(target_os = "linux")]
mod signals;
mod stack;
mod system;
mod ui;

use std::ffi::{c_char, c_void, CStr};
use std::path::PathBuf;
use std::sync::atomic::{AtomicBool, AtomicUsize, Ordering};
use std::collections::HashSet;
use std::sync::{Mutex, OnceLock};

static STARTED: AtomicBool = AtomicBool::new(false);

//Set once the runtime accepts the compilation mask
static COMPILATIONS_REPORTED: AtomicBool = AtomicBool::new(false);

//How many method bodies have been reported, only used for the first line of the log
static COMPILATION_COUNT: AtomicUsize = AtomicUsize::new(0);

//Framework assemblies are left alone when no list is configured
//They carry their own stack probes already, and hooking their entries buys nothing while making every compile slower
//Anything else, the kernel and every mod alike, is fair game: a deep recursion can start anywhere
const FRAMEWORK_PREFIXES: &[&str] = &[
    "System",
    "Microsoft",
    "netstandard",
    "mscorlib",
    "WindowsBase",
    "PresentationCore",
];

//Assemblies that must never be hooked
//
//The first holds the throw entry a trip jumps to, and a hook on it would send every trip back through the check it just
//failed; the second holds the restart that installs this layer and runs before the guard can be armed
const EXCLUDED_ASSEMBLIES: &[&str] = &["NacreCraft.Util", "NacreCraft.Boot"];

//The parsed list, read once on first use
//None means no list was configured, which stands for "everything except the framework"
static REWRITE_LIST: OnceLock<Option<Vec<String>>> = OnceLock::new();

//Module names already reported, so each one only lands in the log once
static SEEN_MODULES: OnceLock<Mutex<HashSet<String>>> = OnceLock::new();

//Whether any assembly has been accepted, and how many have been seen, used to notice a list that matches nothing
static ANY_ACCEPTED: AtomicBool = AtomicBool::new(false);
static SEEN_COUNT: AtomicUsize = AtomicUsize::new(0);

/// ncn_on_initialize runs on the profiling thread before any managed code
/// It is the earliest point this layer gets control, so everything is installed from here
#[no_mangle]
pub extern "C" fn ncn_on_initialize(_profiler_info_unknown: *mut c_void) -> i32 {
    if STARTED.swap(true, Ordering::SeqCst) {
        logging::write("initialize called twice, ignoring the repeat");
        return 0;
    }

    logging::write(&format!(
        "{} initializing, pid {}",
        stack::MODULE_NAME,
        std::process::id()
    ));

    // Collected here rather than on the crash path: reading versions loads libraries and faults there would be fatal
    system::capture();

    // The handler is deliberately not installed here
    // Vectored handlers run newest first, and the runtime registers its own while it starts up, so a handler put in
    // place this early would sit behind it; waiting until the managed side asks for the callback puts this one first

    // The rewrite fulfil thread is the opposite case and may start now: it waits for its own thread to be registered
    // with the runtime before it touches anything
    rejit::start_worker();
    0
}

/// ncn_on_shutdown runs while the runtime is going down
#[no_mangle]
pub extern "C" fn ncn_on_shutdown() {
    logging::write("shutting down");

    //One summary line per run; this is the only moment the counters are guaranteed to still be readable
    ncn_hook_report();
    stack::uninstall();
}

/// ncn_on_jit_notifications reports whether the runtime took the compilation mask
/// Without it no method is ever reported and no entry can be hooked
#[no_mangle]
pub extern "C" fn ncn_on_jit_notifications(taken: bool) {
    COMPILATIONS_REPORTED.store(taken, Ordering::SeqCst);
    if taken {
        logging::write("the runtime took the compilation mask, method entries can be hooked");
    } else {
        logging::write("the runtime refused the compilation mask, method entries cannot be hooked");
    }
}

/// ncn_on_jit_compiled runs for every method body the runtime is about to compile
/// It is called on runtime threads, so anything done here has to be quick and must never throw
#[no_mangle]
pub extern "C" fn ncn_on_jit_compiled(function_id: usize) -> i32 {
    let seen = COMPILATION_COUNT.fetch_add(1, Ordering::Relaxed);
    if seen == 0 {
        logging::write(&format!(
            "the first method body was reported for compilation, function id {function_id:#x}"
        ));
    }
    0
}

/// ncn_stack_hook_enabled answers whether the machine-code guard is on for this run
#[no_mangle]
pub extern "C" fn ncn_stack_hook_enabled() -> i32 {
    if hook::is_enabled() { 1 } else { 0 }
}

/// ncn_stack_hook_ready answers whether the managed throw entry has been handed over
///
/// The shell asks before it looks at a compiled method at all: before this point there is nowhere for a trip to go
#[no_mangle]
pub extern "C" fn ncn_stack_hook_ready() -> i32 {
    if hook::is_ready() { 1 } else { 0 }
}

/// ncn_configure_stack_guard takes the managed throw entry and the reserve size
///
/// A zero entry disarms the guard. The managed side does that first, then resolves the throw entry, then arms it: the
/// lookup compiles the method, and a hook installed on the throw entry would send every trip back through the check.
#[no_mangle]
pub extern "C" fn ncn_configure_stack_guard(trip_entry: usize, reserve: usize) -> i32 {
    hook::configure(trip_entry, reserve)
}

/// ncn_on_method_compiled hands one compiled method to the hook engine
#[no_mangle]
pub unsafe extern "C" fn ncn_on_method_compiled(
    function_id: usize,
    code_start: *const u8,
    code_size: u32,
) -> i32 {
    if code_start.is_null() {
        return -1;
    }

    hook::install(function_id, code_start as usize, code_size);
    0
}

/// ncn_hook_stats copies the counters out
#[no_mangle]
pub unsafe extern "C" fn ncn_hook_stats(out: *mut hook::HookStats) -> i32 {
    if out.is_null() {
        return -1;
    }

    *out = hook::stats();
    0
}

/// ncn_hook_addresses copies the hooked entry addresses out, most recent first is not promised
#[no_mangle]
pub unsafe extern "C" fn ncn_hook_addresses(out: *mut usize, capacity: usize) -> i32 {
    hook::addresses(out, capacity) as i32
}

/// ncn_hook_debug_state reports the TLS values the trampoline and the helper each read
#[no_mangle]
pub unsafe extern "C" fn ncn_hook_debug_state(out: *mut usize, capacity: usize) -> i32 {
    let values = hook::debug_state();
    if out.is_null() || capacity < values.len() {
        return -1;
    }

    for (index, value) in values.iter().enumerate() {
        *out.add(index) = *value;
    }
    0
}

/// ncn_hook_report writes one summary line, used by the harness and by a run that wants the numbers in the log
#[no_mangle]
pub extern "C" fn ncn_hook_report() {
    let stats = hook::stats();
    logging::write(&format!(
        "machine-code guard: {} attempts, {} hooked, {} deduplicated, {} before the entry was armed, \
         {} decode skipped, {} alloc skipped, {} patch skipped, {} trampoline bytes, {} ns installing (max {}), \
         {} slow calls, {} trips, {} thread states, {} threads left unguarded",
        stats.attempts,
        stats.hooked,
        stats.deduplicated,
        stats.not_ready,
        stats.skipped_decode,
        stats.skipped_alloc,
        stats.skipped_patch,
        stats.trampoline_bytes,
        stats.install_nanos,
        stats.max_install_nanos,
        stats.slow_calls,
        stats.trips,
        stats.thread_inits,
        stats.disabled_threads
    ));
}

/// ncn_on_module_seen records one module the layer was asked about, logged once each
/// A run that shows a module missing here means its methods were never offered, which points at the notification
/// rather than the list; a module shown as refused points at the list
#[no_mangle]
pub unsafe extern "C" fn ncn_on_module_seen(simple_name: *const c_char, accepted: bool) {
    if simple_name.is_null() {
        return;
    }

    let Ok(name) = CStr::from_ptr(simple_name).to_str() else {
        return;
    };

    if name.is_empty() {
        return;
    }

    let seen = SEEN_MODULES.get_or_init(|| Mutex::new(HashSet::new()));
    let mut seen = match seen.lock() {
        Ok(guard) => guard,
        Err(_) => return,
    };

    if !seen.insert(name.to_string()) {
        return;
    }

    let verdict = if accepted { "accepted" } else { "refused" };
    logging::write(&format!("module {name}: {verdict}"));

    //A configured list that matches nothing looks exactly like a list that is simply narrow, and the whole mechanism
    //goes quietly dead. Once enough assemblies have gone by without a single match, that is worth saying out loud
    if accepted {
        ANY_ACCEPTED.store(true, Ordering::Relaxed);
        return;
    }

    let seen = SEEN_COUNT.fetch_add(1, Ordering::Relaxed) + 1;
    if seen == 10 && !ANY_ACCEPTED.load(Ordering::Relaxed) {
        logging::write(
            "warning: ten assemblies were seen and the rewrite list matched none of them; \
             check NCN_REWRITE_MODULES, a leftover value will disable the guard",
        );
    }
}

/// ncn_should_rewrite_module answers whether a module's method entries get a hook
/// The list is read from NCN_REWRITE_MODULES, a comma or semicolon separated set of assembly simple names; when the
/// variable is absent a module only qualifies if its assembly references this ecosystem
/// `ecosystem_reference` carries that fact, which the shell reads off the module's metadata and the Rust side cannot see
#[no_mangle]
pub unsafe extern "C" fn ncn_should_rewrite_module(
    simple_name: *const c_char,
    ecosystem_reference: bool,
) -> i32 {
    if simple_name.is_null() {
        return 0;
    }

    let Ok(name) = CStr::from_ptr(simple_name).to_str() else {
        return 0;
    };

    if name.is_empty() {
        return 0;
    }

    if is_rewritable(name, ecosystem_reference) {
        1
    } else {
        0
    }
}

/// is_rewritable decides whether a module's method entries get a hook
///
/// With NCN_REWRITE_MODULES set the list is taken as written and only what it names is hooked, which is how a run can be
/// narrowed for measurement. Without it every assembly counts except the framework ones and the two excluded below: a
/// recursion that runs away can begin in the kernel or in any mod, so leaving mods out would miss exactly the case this
/// is meant to catch.
///
/// The default also requires the module to reference this ecosystem. A third-party library's entries are not this
/// guard's business, and hooking them would put trampolines on code the kernel never wrote
fn is_rewritable(name: &str, ecosystem_reference: bool) -> bool {
    let configured = REWRITE_LIST.get_or_init(|| {
        let resolved = std::env::var("NCN_REWRITE_MODULES")
            .ok()
            .map(|list| {
                list.split([',', ';'])
                    .map(|entry| entry.trim().to_string())
                    .filter(|entry| !entry.is_empty())
                    .collect::<Vec<String>>()
            })
            //A variable that is set but names nothing is treated the same as one that is absent: an empty list would
            //otherwise refuse every assembly, which silently turns the whole mechanism off
            .filter(|list| !list.is_empty());

        //The decision a run makes depends entirely on this value, so it is written down once when it is resolved
        //Without it a run that refuses everything looks the same as one whose list simply does not cover the target
        match &resolved {
            Some(list) => logging::write(&format!("rewrite list resolved to: {}", list.join(", "))),
            None => logging::write(
                "no rewrite list is set, every assembly except the framework ones that references this ecosystem is \
                 eligible",
            ),
        }

        resolved
    });

    match configured {
        Some(list) => list.iter().any(|entry| entry.eq_ignore_ascii_case(name)),
        None => !is_framework(name) && !is_excluded_assembly(name) && ecosystem_reference,
    }
}

/// is_framework reports whether a simple name belongs to a framework assembly
fn is_framework(name: &str) -> bool {
    FRAMEWORK_PREFIXES
        .iter()
        .any(|prefix| name.starts_with(prefix))
}

/// is_excluded_assembly reports whether a simple name must never be hooked
fn is_excluded_assembly(name: &str) -> bool {
    EXCLUDED_ASSEMBLIES
        .iter()
        .any(|excluded| name.eq_ignore_ascii_case(excluded))
}

/// ncn_register_stack_overflow_callback takes the managed handler and the directory its reports belong in
/// The path arrives as UTF-8 bytes. Returns 0 when the callback was taken; without one the native report is still
/// written, so a failure here only costs the richer managed report
#[no_mangle]
pub unsafe extern "C" fn ncn_register_stack_overflow_callback(
    callback: usize,
    report_directory: *const u8,
    length: usize,
) -> i32 {
    //Only the crash report path is registered here now; the guard itself is armed by ncn_configure_stack_guard
    if callback == 0 || report_directory.is_null() || length == 0 {
        return -1;
    }

    let bytes = std::slice::from_raw_parts(report_directory, length);
    let directory = match std::str::from_utf8(bytes) {
        Ok(text) => PathBuf::from(text),
        Err(_) => return -2,
    };

    // The managed side hands over the address of a static method declared with the C calling convention
    let handler: stack::StackOverflowCallback = std::mem::transmute(callback);
    stack::register_callback(handler, directory);

    // Installed here rather than at profiler startup, for the ordering reason noted in ncn_on_initialize
    match stack::install() {
        Ok(()) => logging::write("stack overflow guard installed"),
        Err(message) => logging::write(&format!("stack overflow guard not installed: {message}")),
    }

    0
}

/// ncn_set_product_info records the version line the managed side reports
/// It arrives as UTF-8 bytes; a missing call only leaves the native report without the version block
#[no_mangle]
pub unsafe extern "C" fn ncn_set_product_info(line: *const u8, length: usize) {
    if line.is_null() || length == 0 {
        return;
    }

    let bytes = std::slice::from_raw_parts(line, length);
    if let Ok(text) = std::str::from_utf8(bytes) {
        system::set_product(text.to_string());
    }
}

/// ReportWindowRequest is the layout the managed side fills in
/// Every string arrives as UTF-8 bytes plus its length, and the field order has to stay in step with the C# struct
/// Body carries the text to show directly; report_path carries a file to read it from. A caller supplies one of them,
/// so a failure that never wrote a report can still come up in the window
#[repr(C)]
pub struct ReportWindowRequest {
    pub title: *const u8,
    pub title_length: usize,
    pub summary: *const u8,
    pub summary_length: usize,
    pub body: *const u8,
    pub body_length: usize,
    pub report_path: *const u8,
    pub report_path_length: usize,
    pub label_open_folder: *const u8,
    pub label_open_folder_length: usize,
    pub label_copy: *const u8,
    pub label_copy_length: usize,
    pub label_copied: *const u8,
    pub label_copied_length: usize,
    pub label_close: *const u8,
    pub label_close_length: usize,
    pub blocking: u8,
}

/// read_utf8 copies one string out of a request
unsafe fn read_utf8(pointer: *const u8, length: usize) -> Result<String, i32> {
    if length == 0 {
        return Ok(String::new());
    }
    if pointer.is_null() {
        return Err(-2);
    }

    let bytes = std::slice::from_raw_parts(pointer, length);
    std::str::from_utf8(bytes).map(str::to_owned).map_err(|_| -3)
}

/// ncn_show_report_window puts the error window up for one report
///
/// Returns 0 once the request was taken. With blocking set it returns after the user closes the window; that wait also
/// ends when the window thread gives the request up, so a window that cannot be drawn never keeps the process from
/// shutting down. Any non-zero result means the caller should carry on with the report on disk alone
#[no_mangle]
pub unsafe extern "C" fn ncn_show_report_window(request: *const ReportWindowRequest) -> i32 {
    if request.is_null() {
        return -1;
    }

    let request = &*request;

    let title = match read_utf8(request.title, request.title_length) {
        Ok(value) => value,
        Err(code) => return code,
    };
    let summary = match read_utf8(request.summary, request.summary_length) {
        Ok(value) => value,
        Err(code) => return code,
    };
    let body = match read_utf8(request.body, request.body_length) {
        Ok(value) => value,
        Err(code) => return code,
    };
    let report_path = match read_utf8(request.report_path, request.report_path_length) {
        Ok(value) => value,
        Err(code) => return code,
    };
    let label_open_folder = match read_utf8(request.label_open_folder, request.label_open_folder_length) {
        Ok(value) => value,
        Err(code) => return code,
    };
    let label_copy = match read_utf8(request.label_copy, request.label_copy_length) {
        Ok(value) => value,
        Err(code) => return code,
    };
    let label_copied = match read_utf8(request.label_copied, request.label_copied_length) {
        Ok(value) => value,
        Err(code) => return code,
    };
    let label_close = match read_utf8(request.label_close, request.label_close_length) {
        Ok(value) => value,
        Err(code) => return code,
    };

    // The sender handed to the window comes back through the channel when the window is done with it; it is also
    // dropped when the request never gets shown, which is what keeps a failed window from blocking the caller forever
    let (done, waiting) = if request.blocking != 0 {
        let (sender, receiver) = std::sync::mpsc::channel::<()>();
        (Some(sender), Some(receiver))
    } else {
        (None, None)
    };

    // An empty field means the caller did not supply it: a body to draw as-is, or a report file to read
    let body = if body.is_empty() { None } else { Some(body) };
    let report_path = if report_path.is_empty() {
        None
    } else {
        Some(PathBuf::from(report_path))
    };

    let code = ui::show(ui::Request {
        title,
        summary,
        body,
        report_path,
        label_open_folder,
        label_copy,
        label_copied,
        label_close,
        done,
    });

    if code != 0 {
        return code;
    }

    match waiting {
        Some(receiver) => match receiver.recv() {
            Ok(()) => 0,
            Err(_) => -5,
        },
        None => 0,
    }
}

// The runtime looks these two up by name when it loads the library as a profiler
// They forward into the C++ shell, which is the only side that can build the COM objects the runtime asks for
extern "system" {
    fn ncn_com_get_class_object(rclsid: *const c_void, riid: *const c_void, ppv_object: *mut *mut c_void) -> i32;
    fn ncn_com_can_unload_now() -> i32;
}

#[no_mangle]
pub unsafe extern "system" fn DllGetClassObject(
    rclsid: *const c_void,
    riid: *const c_void,
    ppv_object: *mut *mut c_void,
) -> i32 {
    ncn_com_get_class_object(rclsid, riid, ppv_object)
}

#[no_mangle]
pub unsafe extern "system" fn DllCanUnloadNow() -> i32 {
    ncn_com_can_unload_now()
}
