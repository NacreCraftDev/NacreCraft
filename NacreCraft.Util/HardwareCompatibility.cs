using System.Runtime.InteropServices;
using NacreCraft.Logging;

namespace NacreCraft.Util;

//HardwareCompatibility is the single place declaring which machines NacreCraft refuses and which ones run with known risks
//Startup calls EnsureSupported once; crash reports read the warning list back through DescribeWarnings
public static class HardwareCompatibility
{
    private const int UnsupportedExitCode = 1;

    //Architectures the native layer has no implementation for: all 32-bit, plus wasm which has no native module
    //A machine listed here stops at startup with an error instead of failing later inside the native layer
    private static readonly Architecture[] IncompatibleArchitectures =
    {
        Architecture.X86,
        Architecture.Arm,
        Architecture.Armv6,
        Architecture.Wasm
    };

    //64-bit architectures that can run the managed layer but have no native layer build
    //Playable for the most part, so startup only warns; a native fault there cannot be diagnosed
    private static readonly Architecture[] UnverifiedArchitectures =
    {
        Architecture.LoongArch64,
        Architecture.Ppc64le,
        Architecture.RiscV64,
        Architecture.S390x
    };

    //IsSupported whether the current machine may start at all
    public static bool IsSupported =>
        Environment.Is64BitProcess
        && Array.IndexOf(IncompatibleArchitectures, RuntimeInformation.ProcessArchitecture) < 0;

    //Describe the physical machine: operating system, its version, process architecture and OS architecture
    public static string Describe()
    {
        var process = Name(RuntimeInformation.ProcessArchitecture);
        var machine = Name(RuntimeInformation.OSArchitecture);
        var bitness = Environment.Is64BitProcess ? "64-bit" : "32-bit";
        var emulation = process == machine ? string.Empty : $", emulated on {machine}";
        return $"{RuntimeInformation.OSDescription}, {bitness} process ({process}){emulation}";
    }

    //GetWarnings the conditions that are known to cause failures but do not prevent playing
    public static IReadOnlyList<string> GetWarnings()
    {
        var warnings = new List<string>(3);
        var process = RuntimeInformation.ProcessArchitecture;
        var machine = RuntimeInformation.OSArchitecture;

        //An emulated process cannot use the native layer built for its instruction set; reuse of that code is untested
        if (process != machine)
            warnings.Add($"{Name(process)} process emulated on {Name(machine)}; "
                + "native layer behaviour under emulation is not verified");

        if (Array.IndexOf(UnverifiedArchitectures, process) >= 0)
            warnings.Add($"{Name(process)} has no native layer build; native faults cannot be reported");

        if (!OperatingSystem.IsWindows() && !OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
            warnings.Add($"{RuntimeInformation.OSDescription} has no native layer build");

        return warnings;
    }

    //DescribeWarnings the warning list as one line for crash reports
    public static string DescribeWarnings()
    {
        var warnings = GetWarnings();
        return warnings.Count == 0 ? "none" : string.Join("; ", warnings);
    }

    //EnsureSupported terminates the process on an incompatible machine and warns about known risks otherwise
    //Every native library is loaded after this point, so an incompatible machine never reaches them
    public static void EnsureSupported()
    {
        if (!IsSupported)
        {
            Log.Error($"Unsupported runtime environment: {Describe()}. "
                + "NacreCraft requires a 64-bit process on x64 or arm64.");
            Log.Flush();
            Environment.Exit(UnsupportedExitCode);
        }

        foreach (var warning in GetWarnings())
            Log.Warning($"Compatibility: {warning}");
    }

    private static string Name(Architecture architecture) => architecture switch
    {
        Architecture.X64 => "x64",
        Architecture.X86 => "x86",
        Architecture.Arm64 => "arm64",
        Architecture.Arm => "arm",
        Architecture.Armv6 => "armv6",
        Architecture.Wasm => "wasm",
        Architecture.S390x => "s390x",
        Architecture.LoongArch64 => "loongarch64",
        Architecture.Ppc64le => "ppc64le",
        Architecture.RiscV64 => "riscv64",
        _ => architecture.ToString().ToLowerInvariant()
    };
}
