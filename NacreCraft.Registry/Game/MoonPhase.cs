using NacreCraft.Codec;

namespace NacreCraft.Registry;

//MoonPhase moon phase, maps to vanilla net.minecraft.world.level.MoonPhase
//The index runs from a full moon to a waning gibbous, one phase per 24000 ticks
public enum MoonPhase
{
    FullMoon,
    WaningGibbous,
    ThirdQuarter,
    WaningCrescent,
    NewMoon,
    WaxingCrescent,
    FirstQuarter,
    WaxingGibbous
}

//MoonPhases moon phase helpers and codec
public static class MoonPhases
{
    //Count number of phases in a full cycle, maps to vanilla MoonPhase.COUNT
    public const int Count = 8;

    //PhaseLength tick length of one phase, maps to vanilla MoonPhase.PHASE_LENGTH
    public const int PhaseLength = 24000;

    public static readonly Codec<MoonPhase> Codec = new StringEnumCodec<MoonPhase>(
        (MoonPhase.FullMoon, "full_moon"),
        (MoonPhase.WaningGibbous, "waning_gibbous"),
        (MoonPhase.ThirdQuarter, "third_quarter"),
        (MoonPhase.WaningCrescent, "waning_crescent"),
        (MoonPhase.NewMoon, "new_moon"),
        (MoonPhase.WaxingCrescent, "waxing_crescent"),
        (MoonPhase.FirstQuarter, "first_quarter"),
        (MoonPhase.WaxingGibbous, "waxing_gibbous"));

    //Index the phase's position in the cycle
    public static int Index(this MoonPhase phase) => (int)phase;

    //StartTick tick within the cycle where the phase begins
    public static int StartTick(this MoonPhase phase) => (int)phase * PhaseLength;
}
