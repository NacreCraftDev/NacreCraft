using NacreCraft.Codec;
using NacreCraft.Game.Commands.Arguments;
using NacreCraft.Registry;
using NacreCraft.Registry.Environment;
using GameDimensionType = NacreCraft.Game.World.Level.LevelGen.Dimension.DimensionType;
using RegistrySpawnCondition = NacreCraft.Registry.SpawnCondition;

namespace NacreCraft.Game.World.Entity.Variant;

//MoonBrightnessCheck requires the moon brightness at the position to fall in the range, maps to vanilla net.minecraft.world.entity.variant.MoonBrightnessCheck
public sealed record MoonBrightnessCheck(MinMaxBounds.Doubles Range) : SpawnCondition
{
    public static readonly MapCodec<MoonBrightnessCheck> MAP_CODEC = RecordCodecBuilder.Of1(
        MinMaxBounds.Doubles.CODEC.FieldOf("range").ForGetter((MoonBrightnessCheck check) => check.Range),
        range => new MoonBrightnessCheck(range));

    public static readonly MapCodec<RegistrySpawnCondition> ConditionCodec =
        new XmapMapCodec<MoonBrightnessCheck, RegistrySpawnCondition>(MAP_CODEC, value => value,
            condition => (MoonBrightnessCheck)condition);

    public MapCodec<RegistrySpawnCondition> Codec() => ConditionCodec;

    public bool Test(SpawnContext context)
    {
        var moonPhase = context.EnvironmentAttributes.GetValue(EnvironmentAttributes.MOON_PHASE, context.Pos);
        return Range.Matches(GameDimensionType.MOON_BRIGHTNESS_PER_PHASE[moonPhase.Index()]);
    }
}
