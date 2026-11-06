using System.Globalization;
using NacreCraft.Game.Server;
using NacreCraft.Game.World.Entity;
using NacreCraft.Network.Chat;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;
using RegEntity = NacreCraft.Registry.Entity;
using NacreCraft.Util;

namespace NacreCraft.Game.World.Effect;

//MobEffectUtil effect helpers shared by gameplay code, maps to vanilla net.minecraft.world.effect.MobEffectUtil
public static class MobEffectUtil
{
    //FormatDuration renders the remaining duration for tooltips, maps to vanilla formatDuration
    //The duration is scaled by the effect's duration scale and formatted at the given tick rate
    public static Component FormatDuration(MobEffectInstance instance, float scale, float tickRate)
        => instance.IsInfiniteDuration
            ? Component.Translatable("effect.duration.infinite")
            : Component.Literal(FormatTickDuration(Mth.Floor(instance.Duration * scale), tickRate));

    //FormatTickDuration renders ticks as mm:ss or hh:mm:ss, maps to vanilla StringUtil.formatTickDuration
    public static string FormatTickDuration(int ticks, float tickRate)
    {
        var seconds = Mth.Floor(ticks / tickRate);
        var minutes = seconds / 60;
        var remainingSeconds = seconds % 60;
        var hours = minutes / 60;
        var remainingMinutes = minutes % 60;
        return hours > 0
            ? string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}:{2:00}", hours, remainingMinutes, remainingSeconds)
            : string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}", remainingMinutes, remainingSeconds);
    }

    //HasDigSpeed whether the entity digs faster, maps to vanilla hasDigSpeed
    public static bool HasDigSpeed(LivingEntity mob)
        => mob.HasEffect(MobEffects.HASTE) || mob.HasEffect(MobEffects.CONDUIT_POWER);

    //GetDigSpeedAmplification the strongest haste-like level, maps to vanilla getDigSpeedAmplification
    public static int GetDigSpeedAmplification(LivingEntity mob)
    {
        var haste = 0;
        var conduit = 0;
        if (mob.GetEffect(MobEffects.HASTE) is { } hasteEffect) haste = hasteEffect.Amplifier;
        if (mob.GetEffect(MobEffects.CONDUIT_POWER) is { } conduitEffect) conduit = conduitEffect.Amplifier;
        return Math.Max(haste, conduit);
    }

    //HasWaterBreathing whether the entity can breathe underwater, maps to vanilla hasWaterBreathing
    public static bool HasWaterBreathing(LivingEntity mob)
        => mob.HasEffect(MobEffects.WATER_BREATHING) || mob.HasEffect(MobEffects.CONDUIT_POWER)
           || mob.HasEffect(MobEffects.BREATH_OF_THE_NAUTILUS);

    //ShouldEffectsRefillAirsupply whether the air supply refills this tick, maps to vanilla shouldEffectsRefillAirsupply
    //Breath of the nautilus stops the refill unless water breathing or conduit power also applies
    public static bool ShouldEffectsRefillAirsupply(LivingEntity mob)
        => !mob.HasEffect(MobEffects.BREATH_OF_THE_NAUTILUS)
           || mob.HasEffect(MobEffects.WATER_BREATHING) || mob.HasEffect(MobEffects.CONDUIT_POWER);

    //AddEffectToPlayersAround applies the effect to nearby survival players, maps to vanilla addEffectToPlayersAround
    //Vanilla asks the level for its player list; the level layer here holds no player list, so the caller passes the online
    //players. Team allies are not modelled, so only the source itself is excluded
    public static List<ServerPlayer> AddEffectToPlayersAround(ServerLevel level, IEnumerable<ServerPlayer> players,
        RegEntity? source, Vec3 position, double radius, MobEffectInstance effectInstance, int displayEffectLimit)
    {
        var effect = effectInstance.Effect;
        var matched = new List<ServerPlayer>();
        foreach (var player in players)
        {
            if (!player.GameType.IsSurvival) continue;
            if (source is not null && ReferenceEquals(source, player)) continue;
            if (position.DistanceTo(player.Pos) >= radius) continue;
            var existing = player.GetEffect(effect);
            if (existing is not null && existing.Amplifier >= effectInstance.Amplifier
                                   && !existing.EndsWithin(displayEffectLimit - 1)) continue;
            matched.Add(player);
        }
        foreach (var player in matched) player.AddEffect(new MobEffectInstance(effectInstance));
        return matched;
    }
}
