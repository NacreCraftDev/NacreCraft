using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;
using NacreCraft.Config;

namespace NacreCraft.Util;

//SystemReport the System Details section, maps to vanilla net.minecraft.SystemReport
//It is the System Details section at the end of a crash report; hardware info only covers what the managed runtime exposes
//Each item is individually error-guarded; a failure collecting info must not prevent the crash report from being written
public sealed class SystemReport
{
    private const long BytesPerMebibyte = 1024 * 1024;

    private readonly List<(string Key, string Value)> _entries = new();

    public SystemReport()
    {
        SetDetail("NacreCraft Version", SharedConstants.Version);
        SetDetail("Protocol Version", SharedConstants.ProtocolVersion.ToString());
        SetDetail("World Data Version", SharedConstants.WorldDataVersion.ToString());
        SetDetail("Operating System", $"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})");
        SetDetail("Windows Build", DescribeWindowsBuild);
        SetDetail("Emulation", DescribeEmulation);
        SetDetail("Runtime", RuntimeInformation.FrameworkDescription);
        SetDetail("Runtime Identifier", RuntimeInformation.RuntimeIdentifier);
        SetDetail("MSVC Runtime", DescribeMsvcRuntime);
        SetDetail("Mitigations", DescribeMitigations);
        SetDetail("Code Integrity", DescribeCodeIntegrity);
        SetDetail("Native Layer", DescribeNativeLayer);
        SetDetail("Process", DescribeProcess);
        SetDetail("Memory", DescribeMemory);
        SetDetail("Memory (GC)", DescribeGcMemory);
        SetDetail("CPUs", Environment.ProcessorCount.ToString);
        SetDetail("Storage (workdir)", () => DescribeStorage(AppContext.BaseDirectory));
        SetDetail("Debug Flags", DescribeDebugFlags);
        SetDetail("Loaded Modules", DescribeLoadedModules);
    }

    //SetDetail writes one entry directly
    public void SetDetail(string key, string value) => _entries.Add((key, value));

    //SetDetail degrades to an error note when value retrieval fails, maps to the catch branch of vanilla setDetail
    public void SetDetail(string key, Func<string> valueSupplier)
    {
        try
        {
            _entries.Add((key, valueSupplier()));
        }
        catch (Exception e)
        {
            _entries.Add((key, $"~~ERROR~~ {e.GetType().Name}: {e.Message}"));
        }
    }

    //AppendToCrashReportString appends the System Details section in vanilla format
    public void AppendToCrashReportString(StringBuilder builder)
    {
        var newLine = Environment.NewLine;
        builder.Append("-- System Details --").Append(newLine);
        builder.Append("Details:");
        foreach (var (key, value) in _entries)
            builder.Append(newLine).Append('\t').Append(key).Append(": ").Append(value);
    }

    //ToLineSeparatedString one key-value pair per line, for logs and diagnostic commands
    public string ToLineSeparatedString()
        => string.Join(Environment.NewLine, _entries.Select(e => $"{e.Key}: {e.Value}"));

    //DescribeProcess process uptime, memory usage, thread count and bitness
    private static string DescribeProcess()
    {
        using var process = Process.GetCurrentProcess();
        var uptime = (DateTime.Now - process.StartTime).TotalSeconds;
        return $"Uptime: {uptime:F0}s, working set: {process.WorkingSet64 / BytesPerMebibyte} MiB, "
            + $"private: {process.PrivateMemorySize64 / BytesPerMebibyte} MiB, threads: {process.Threads.Count}, "
            + $"{(Environment.Is64BitProcess ? "64" : "32")}-bit";
    }

    //DescribeMemory managed heap used and runtime available limit, maps to the vanilla Runtime memory note
    private static string DescribeMemory()
    {
        var info = GC.GetGCMemoryInfo();
        var used = GC.GetTotalMemory(false);
        return $"{used} bytes({used / BytesPerMebibyte} MiB) used / "
            + $"{info.TotalAvailableMemoryBytes} bytes({info.TotalAvailableMemoryBytes / BytesPerMebibyte} MiB) available";
    }

    //DescribeGcMemory heap committed and fragmentation, and whether GC is workstation or server
    private static string DescribeGcMemory()
    {
        var info = GC.GetGCMemoryInfo();
        return $"heap: {info.HeapSizeBytes / BytesPerMebibyte} MiB, committed: {info.TotalCommittedBytes / BytesPerMebibyte} MiB, "
            + $"fragmented: {info.FragmentedBytes / BytesPerMebibyte} MiB, pinned objects: {info.PinnedObjectsCount}, "
            + $"GC mode: {(GCSettings.IsServerGC ? "server" : "workstation")}, latency: {GCSettings.LatencyMode}";
    }

    //DescribeStorage free and total space of the disk holding the program root, maps to the vanilla workdir space note
    private static string DescribeStorage(string path)
    {
        var root = Path.GetPathRoot(Path.GetFullPath(path));
        if (string.IsNullOrEmpty(root)) return "<no such root>";
        var drive = new DriveInfo(root);
        if (!drive.IsReady) return "<drive not ready>";
        return $"available: {drive.AvailableFreeSpace / BytesPerMebibyte} MiB, total: {drive.TotalSize / BytesPerMebibyte} MiB";
    }

    //DescribeDebugFlags the currently effective debug flags, maps to the vanilla Debug Flags note
    private static string DescribeDebugFlags()
    {
        var enabled = new List<string>();
        if (DebugMode.IsEnabled) enabled.Add("debug");
        return enabled.Count == 0 ? "0 total" : $"{enabled.Count} total; {string.Join(" ", enabled)}";
    }

    //DescribeWindowsBuild the exact build and UBR, which the runtime's OS string omits
    private static string DescribeWindowsBuild()
    {
        if (!OperatingSystem.IsWindows()) return "<not Windows>";
        var info = new OsVersionInfo { Size = Marshal.SizeOf<OsVersionInfo>() };
        if (RtlGetVersion(ref info) != 0) return "<unavailable>";
        var revision = RegistryDword(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "UBR");
        return revision is null
            ? $"{info.Major}.{info.Minor}.{info.Build}"
            : $"{info.Major}.{info.Minor}.{info.Build}.{revision}";
    }

    //DescribeEmulation the machine the process was built for against the native machine
    //A native machine different from the process machine means the runtime is under emulation
    private static string DescribeEmulation()
    {
        if (!OperatingSystem.IsWindows()) return "<not Windows>";
        if (!IsWow64Process2(GetCurrentProcess(), out var processMachine, out var nativeMachine))
            return "<unavailable>";
        return $"process {MachineName(processMachine)}, native {MachineName(nativeMachine)}";
    }

    private static string MachineName(ushort machine) => machine switch
    {
        0x014C => "x86",
        0x8664 => "AMD64",
        0xAA64 => "ARM64",
        0x01C4 => "ARM32",
        0 => "unknown",
        _ => $"0x{machine:X4}"
    };

    //DescribeMsvcRuntime the system C++ runtime versions, since a mismatch with the build toolchain changes STL behaviour
    private static string DescribeMsvcRuntime()
    {
        if (!OperatingSystem.IsWindows()) return "<not Windows>";
        var root = Environment.GetFolderPath(Environment.SpecialFolder.System);
        var parts = new List<string>(3);
        foreach (var name in new[] { "MSVCP140.dll", "VCRUNTIME140.dll", "VCRUNTIME140_1.dll" })
        {
            var path = Path.Combine(root, name);
            if (!File.Exists(path))
            {
                parts.Add($"{Path.GetFileNameWithoutExtension(name)} absent");
                continue;
            }
            var version = FileVersionInfo.GetVersionInfo(path).FileVersion;
            parts.Add($"{Path.GetFileNameWithoutExtension(name)} {version ?? "unknown"}");
        }
        return string.Join(", ", parts);
    }

    //DescribeMitigations the exploit mitigations applied to this process
    //A prohibited dynamic code policy blocks ReJIT before the runtime reaches any rewrite
    private static string DescribeMitigations()
    {
        if (!OperatingSystem.IsWindows()) return "<not Windows>";
        var process = GetCurrentProcess();
        var parts = new List<string>(4);

        if (GetProcessDEPPolicy(process, out var dep, out _))
            parts.Add($"DEP {((dep & 1) != 0 ? "on" : "off")}");

        if (GetProcessMitigationPolicy(process, ProcessControlFlowGuardPolicy, out var cfg, (UIntPtr)sizeof(uint)))
            parts.Add($"CFG {((cfg & 1) != 0 ? "on" : "off")}{((cfg & 0b100) != 0 ? ", strict" : "")}");

        if (GetProcessMitigationPolicy(process, ProcessDynamicCodePolicy, out var dynamicCode, (UIntPtr)sizeof(uint)))
            parts.Add($"dynamic code {((dynamicCode & 1) != 0 ? "prohibited" : "allowed")}");

        if (GetProcessMitigationPolicy(process, ProcessSignaturePolicy, out var signature, (UIntPtr)sizeof(uint)))
        {
            var note = (signature & 1) != 0 ? "Microsoft-signed only"
                : (signature & 2) != 0 ? "store-signed only" : "none";
            parts.Add($"signature {note}");
        }

        return parts.Count == 0 ? "<unavailable>" : string.Join(", ", parts);
    }

    //DescribeCodeIntegrity whether the hypervisor enforces code integrity, which refuses modified executable memory
    private static string DescribeCodeIntegrity()
    {
        if (!OperatingSystem.IsWindows()) return "<not Windows>";
        var hvci = RegistryDword(
            @"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "Enabled");
        var vbs = RegistryDword(@"SYSTEM\CurrentControlSet\Control\DeviceGuard", "EnableVirtualizationBasedSecurity");
        return $"HVCI: {DescribeToggle(hvci)}, VBS: {DescribeToggle(vbs)}";
    }

    private static string DescribeToggle(uint? value) => value switch
    {
        1 => "enabled",
        0 => "disabled",
        _ => "not configured"
    };

    //DescribeNativeLayer the loaded native layer with its size, version and digest
    private static string DescribeNativeLayer()
    {
        using var process = Process.GetCurrentProcess();
        foreach (ProcessModule module in process.Modules)
        {
            if (module.ModuleName?.Contains("nacrecraft_native", StringComparison.OrdinalIgnoreCase) != true) continue;
            var path = module.FileName;
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return module.ModuleName ?? "<unnamed>";
            var info = new FileInfo(path);
            var version = FileVersionInfo.GetVersionInfo(path).FileVersion ?? "unknown";
            var digest = info.Length <= MaxDigestBytes
                ? Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant()
                : "<too large to hash>";
            return $"{path} ({info.Length} bytes, version {version}, sha256 {digest})";
        }
        return "<not loaded>";
    }

    //RegistryDword reads one DWORD value, returning null when the key or value is absent
    private static uint? RegistryDword(string subkey, string name)
    {
        if (!OperatingSystem.IsWindows()) return null;
        using var key = Registry.LocalMachine.OpenSubKey(subkey);
        return key?.GetValue(name) is int value ? (uint)value : null;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct OsVersionInfo
    {
        public int Size;
        public int Major;
        public int Minor;
        public int Build;
        public int Platform;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string ServicePack;
    }

    [DllImport("ntdll.dll")]
    private static extern int RtlGetVersion(ref OsVersionInfo info);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool IsWow64Process2(IntPtr process, out ushort processMachine, out ushort nativeMachine);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetProcessDEPPolicy(IntPtr process, out uint flags, out int permanent);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetProcessMitigationPolicy(IntPtr process, int policy, out uint flags, UIntPtr size);

    private const int ProcessDynamicCodePolicy = 2;
    private const int ProcessControlFlowGuardPolicy = 7;
    private const int ProcessSignaturePolicy = 8;
    private const long MaxDigestBytes = 64L * 1024 * 1024;

    //DescribeLoadedModules every module mapped into the process, native and managed alike, sorted for comparison
    //A library absent from this list was never found: that is what separates an unresolved dependency from a broken one
    private static string DescribeLoadedModules()
    {
        using var process = Process.GetCurrentProcess();
        var modules = new List<string>(process.Modules.Count);
        foreach (ProcessModule module in process.Modules)
            modules.Add(string.IsNullOrEmpty(module.FileName) ? module.ModuleName : module.FileName);
        modules.Sort(StringComparer.OrdinalIgnoreCase);

        var builder = new StringBuilder();
        builder.Append(modules.Count).Append(" loaded");
        foreach (var module in modules)
            builder.Append(Environment.NewLine).Append("\t\t").Append(module);
        return builder.ToString();
    }
}
