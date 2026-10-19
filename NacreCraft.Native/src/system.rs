//! The system fingerprint carried into a native crash report
//!
//! Everything here is collected once, when the profiler is initialised, and stored as plain text. The crash path only
//! copies that text: reading files, loading libraries or allocating during a hardware fault can deadlock or fault again.
//!
//! The fields target the cases the fault report cannot answer on its own: which binaries are loaded, whether the
//! process is running under emulation, which exploit mitigations the system applied, and whether code integrity is
//! enforcing anything.

use std::sync::OnceLock;

static INFO: OnceLock<String> = OnceLock::new();

/// capture collects the fingerprint; the first call wins and later ones are ignored
pub fn capture() {
    let _ = INFO.set(collect());
}

/// describe returns the cached fingerprint, or a note when capture never ran
pub fn describe() -> &'static str {
    INFO.get().map(String::as_str).unwrap_or("the system fingerprint was not captured")
}

#[cfg(windows)]
fn collect() -> String {
    win::collect()
}

#[cfg(not(windows))]
fn collect() -> String {
    format!(
        "OS: {}\nArchitecture: {}\n",
        std::env::consts::OS,
        std::env::consts::ARCH
    )
}

#[cfg(windows)]
mod win {
    use std::ffi::c_void;
    use std::fs::File;
    use std::io::{Read, Seek, SeekFrom};

    const GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS: u32 = 0x0000_0004;
    const GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT: u32 = 0x0000_0002;
    const HKEY_LOCAL_MACHINE: *mut c_void = 0x8000_0002u32 as usize as *mut c_void;
    const RRF_RT_REG_DWORD: u32 = 0x0000_0010;
    const PROCESS_DYNAMIC_CODE_POLICY: i32 = 2;
    const PROCESS_CONTROL_FLOW_GUARD_POLICY: i32 = 7;
    const PROCESS_SIGNATURE_POLICY: i32 = 8;

    #[repr(C)]
    struct OsVersionInfoW {
        size: u32,
        major: u32,
        minor: u32,
        build: u32,
        platform: u32,
        csd: [u16; 128],
    }

    #[repr(C)]
    struct FixedFileInfo {
        signature: u32,
        struc_version: u32,
        file_version_ms: u32,
        file_version_ls: u32,
        product_version_ms: u32,
        product_version_ls: u32,
        file_flags_mask: u32,
        file_flags: u32,
        file_os: u32,
        file_type: u32,
        file_subtype: u32,
        file_date_ms: u32,
        file_date_ls: u32,
    }

    #[link(name = "ntdll")]
    extern "system" {
        fn RtlGetVersion(info: *mut OsVersionInfoW) -> i32;
    }

    #[link(name = "kernel32")]
    extern "system" {
        fn GetSystemDirectoryW(buffer: *mut u16, size: u32) -> u32;
        fn GetModuleHandleExW(flags: u32, name: *const u16, module: *mut *mut c_void) -> i32;
        fn GetModuleFileNameW(module: *mut c_void, buffer: *mut u16, size: u32) -> u32;
        fn GetCurrentProcess() -> *mut c_void;
        fn GetProcessDEPPolicy(process: *mut c_void, flags: *mut u32, permanent: *mut i32) -> i32;
        fn LoadLibraryW(name: *const u16) -> *mut c_void;
        fn GetProcAddress(module: *mut c_void, name: *const u8) -> *mut c_void;
    }

    type GetFileVersionInfoSizeW = unsafe extern "system" fn(*const u16, *mut u32) -> u32;
    type GetFileVersionInfoW = unsafe extern "system" fn(*const u16, u32, u32, *mut c_void) -> i32;
    type VerQueryValueW = unsafe extern "system" fn(*const c_void, *const u16, *mut *mut c_void, *mut u32) -> i32;
    type IsWow64Process2Fn = unsafe extern "system" fn(*mut c_void, *mut u16, *mut u16) -> i32;
    type GetProcessMitigationPolicyFn = unsafe extern "system" fn(*mut c_void, i32, *mut u32, usize) -> i32;
    type RegGetValueW = unsafe extern "system" fn(
        *mut c_void,
        *const u16,
        *const u16,
        u32,
        *mut u32,
        *mut c_void,
        *mut u32,
    ) -> i32;

    fn wide(text: &str) -> Vec<u16> {
        text.encode_utf16().chain(std::iter::once(0)).collect()
    }

    fn symbol(module: *mut c_void, name: &str) -> Option<*mut c_void> {
        if module.is_null() {
            return None;
        }
        let mut bytes = name.as_bytes().to_vec();
        bytes.push(0);
        let address = unsafe { GetProcAddress(module, bytes.as_ptr()) };
        if address.is_null() {
            None
        } else {
            Some(address)
        }
    }

    fn library(name: &str) -> *mut c_void {
        unsafe { LoadLibraryW(wide(name).as_ptr()) }
    }

    fn os_version() -> Option<(u32, u32, u32)> {
        let mut info = OsVersionInfoW {
            size: std::mem::size_of::<OsVersionInfoW>() as u32,
            major: 0,
            minor: 0,
            build: 0,
            platform: 0,
            csd: [0; 128],
        };
        if unsafe { RtlGetVersion(&mut info) } != 0 {
            return None;
        }
        Some((info.major, info.minor, info.build))
    }

    fn registry_dword(subkey: &str, value: &str) -> Option<u32> {
        let advapi = library("advapi32.dll");
        let function: RegGetValueW = unsafe { std::mem::transmute(symbol(advapi, "RegGetValueW")?) };
        let mut data: u32 = 0;
        let mut size: u32 = std::mem::size_of::<u32>() as u32;
        let subkey = wide(subkey);
        let value = wide(value);
        let status = unsafe {
            function(
                HKEY_LOCAL_MACHINE,
                subkey.as_ptr(),
                value.as_ptr(),
                RRF_RT_REG_DWORD,
                std::ptr::null_mut(),
                &mut data as *mut u32 as *mut c_void,
                &mut size,
            )
        };
        if status == 0 {
            Some(data)
        } else {
            None
        }
    }

    /// file_version reads the fixed file version out of a PE's resource section
    fn file_version(path: &str) -> Option<String> {
        let version = library("version.dll");
        let size_fn: GetFileVersionInfoSizeW =
            unsafe { std::mem::transmute(symbol(version, "GetFileVersionInfoSizeW")?) };
        let info_fn: GetFileVersionInfoW =
            unsafe { std::mem::transmute(symbol(version, "GetFileVersionInfoW")?) };
        let query_fn: VerQueryValueW =
            unsafe { std::mem::transmute(symbol(version, "VerQueryValueW")?) };

        let path = wide(path);
        let mut ignored: u32 = 0;
        let size = unsafe { size_fn(path.as_ptr(), &mut ignored) };
        if size == 0 {
            return None;
        }

        let mut buffer = vec![0u8; size as usize];
        if unsafe { info_fn(path.as_ptr(), 0, size, buffer.as_mut_ptr() as *mut c_void) } == 0 {
            return None;
        }

        let root = wide("\\");
        let mut fixed: *mut c_void = std::ptr::null_mut();
        let mut length: u32 = 0;
        if unsafe {
            query_fn(
                buffer.as_ptr() as *const c_void,
                root.as_ptr(),
                &mut fixed,
                &mut length,
            )
        } == 0
            || fixed.is_null()
        {
            return None;
        }

        let info = unsafe { &*(fixed as *const FixedFileInfo) };
        Some(format!(
            "{}.{}.{}.{}",
            info.file_version_ms >> 16,
            info.file_version_ms & 0xFFFF,
            info.file_version_ls >> 16,
            info.file_version_ls & 0xFFFF
        ))
    }

    /// system_dll builds the path of a library that ships with Windows
    fn system_dll(name: &str) -> Option<String> {
        let mut buffer = [0u16; 260];
        let length = unsafe { GetSystemDirectoryW(buffer.as_mut_ptr(), buffer.len() as u32) };
        if length == 0 || length as usize >= buffer.len() {
            return None;
        }
        let directory = String::from_utf16_lossy(&buffer[..length as usize]);
        Some(format!("{directory}\\{name}"))
    }

    /// pe_timestamp reads the linker timestamp, which changes with every build of a binary
    fn pe_timestamp(path: &str) -> Option<u32> {
        let mut file = File::open(path).ok()?;
        let mut raw = [0u8; 4];
        file.seek(SeekFrom::Start(0x3C)).ok()?;
        file.read_exact(&mut raw).ok()?;
        let header = u32::from_le_bytes(raw) as u64;
        file.seek(SeekFrom::Start(header + 8)).ok()?;
        file.read_exact(&mut raw).ok()?;
        Some(u32::from_le_bytes(raw))
    }

    /// own_module returns the path and size of the library this code lives in
    fn own_module() -> Option<(String, u64)> {
        let mut module: *mut c_void = std::ptr::null_mut();
        let address = collect as *const () as *const u16;
        if unsafe {
            GetModuleHandleExW(
                GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
                address,
                &mut module,
            )
        } == 0
        {
            return None;
        }

        let mut buffer = [0u16; 1024];
        let length = unsafe { GetModuleFileNameW(module, buffer.as_mut_ptr(), buffer.len() as u32) };
        if length == 0 {
            return None;
        }
        let path = String::from_utf16_lossy(&buffer[..length as usize]);
        let size = std::fs::metadata(&path).map(|meta| meta.len()).unwrap_or(0);
        Some((path, size))
    }

    fn machine_name(machine: u16) -> &'static str {
        match machine {
            0x014C => "x86",
            0x8664 => "AMD64",
            0xAA64 => "ARM64",
            0x01C4 => "ARM32",
            0 => "unknown",
            _ => "other",
        }
    }

    fn emulation() -> String {
        let kernel = library("kernel32.dll");
        let Some(address) = symbol(kernel, "IsWow64Process2") else {
            return "emulation: IsWow64Process2 unavailable".to_string();
        };
        let function: IsWow64Process2Fn = unsafe { std::mem::transmute(address) };
        let mut process_machine: u16 = 0;
        let mut native_machine: u16 = 0;
        if unsafe { function(GetCurrentProcess(), &mut process_machine, &mut native_machine) } == 0 {
            return "emulation: unknown".to_string();
        }
        format!(
            "Process machine: {}, native machine: {}",
            machine_name(process_machine),
            machine_name(native_machine)
        )
    }

    fn mitigation(policy: i32) -> Option<u32> {
        let kernel = library("kernel32.dll");
        let function: GetProcessMitigationPolicyFn =
            unsafe { std::mem::transmute(symbol(kernel, "GetProcessMitigationPolicy")?) };
        let mut flags: u32 = 0;
        let status = unsafe { function(GetCurrentProcess(), policy, &mut flags, std::mem::size_of::<u32>()) };
        if status != 0 {
            Some(flags)
        } else {
            None
        }
    }

    fn mitigations() -> String {
        let mut parts = Vec::new();

        let mut dep_flags: u32 = 0;
        let mut permanent: i32 = 0;
        if unsafe { GetProcessDEPPolicy(GetCurrentProcess(), &mut dep_flags, &mut permanent) } != 0 {
            parts.push(format!("DEP {}", if dep_flags & 1 != 0 { "on" } else { "off" }));
        }

        if let Some(flags) = mitigation(PROCESS_CONTROL_FLOW_GUARD_POLICY) {
            let strict = if flags & 0b100 != 0 { ", strict" } else { "" };
            parts.push(format!("CFG {}{strict}", if flags & 1 != 0 { "on" } else { "off" }));
        }

        if let Some(flags) = mitigation(PROCESS_DYNAMIC_CODE_POLICY) {
            parts.push(format!(
                "dynamic code {}",
                if flags & 1 != 0 { "prohibited" } else { "allowed" }
            ));
        }

        if let Some(flags) = mitigation(PROCESS_SIGNATURE_POLICY) {
            let note = if flags & 1 != 0 {
                "Microsoft-signed only"
            } else if flags & 2 != 0 {
                "store-signed only"
            } else {
                "none"
            };
            parts.push(format!("signature {note}"));
        }

        if parts.is_empty() {
            "mitigations: unavailable".to_string()
        } else {
            format!("Mitigations: {}", parts.join(", "))
        }
    }

    fn code_integrity() -> String {
        let hvci = registry_dword(
            "SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\HypervisorEnforcedCodeIntegrity",
            "Enabled",
        );
        let vbs = registry_dword(
            "SYSTEM\\CurrentControlSet\\Control\\DeviceGuard",
            "EnableVirtualizationBasedSecurity",
        );
        let describe = |value: Option<u32>| match value {
            Some(1) => "enabled",
            Some(0) => "disabled",
            _ => "not configured",
        };
        format!(
            "HVCI: {}, VBS: {}",
            describe(hvci),
            describe(vbs)
        )
    }

    fn msvc() -> String {
        let names = ["MSVCP140.dll", "VCRUNTIME140.dll", "VCRUNTIME140_1.dll"];
        let mut parts = Vec::new();
        for name in names {
            let Some(path) = system_dll(name) else {
                parts.push(format!("{name} absent"));
                continue;
            };
            match file_version(&path) {
                Some(version) => parts.push(format!("{} {version}", name.trim_end_matches(".dll"))),
                None => parts.push(format!("{name} absent")),
            }
        }
        format!("MSVC runtime: {}", parts.join(", "))
    }

    fn native_layer() -> String {
        let Some((path, size)) = own_module() else {
            return "Native layer: unknown".to_string();
        };
        let version = file_version(&path).unwrap_or_else(|| "unknown".to_string());
        let timestamp = pe_timestamp(&path)
            .map(|value| format!("0x{value:08X}"))
            .unwrap_or_else(|| "unknown".to_string());
        format!(
            "Native layer: {path} ({size} bytes, version {version}, timestamp {timestamp})"
        )
    }

    pub fn collect() -> String {
        let mut lines = Vec::new();

        match os_version() {
            Some((major, minor, build)) => {
                let revision = registry_dword(
                    "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion",
                    "UBR",
                );
                match revision {
                    Some(ubr) => lines.push(format!("OS: Windows {major}.{minor}.{build}.{ubr}")),
                    None => lines.push(format!("OS: Windows {major}.{minor}.{build}")),
                }
            }
            None => lines.push("OS: unknown".to_string()),
        }

        lines.push(emulation());
        lines.push(msvc());
        lines.push(mitigations());
        lines.push(code_integrity());
        lines.push(native_layer());

        lines.join("\n")
    }
}
