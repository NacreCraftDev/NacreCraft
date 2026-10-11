using NetCraft.Game.World.Items;
using NetCraft.Game.World.Level.GameEvents;
using NetCraft.Registry.State;

namespace NetCraft.Game.World.Particle;

//ParticleOptions particle option base class, maps to vanilla net.minecraft.core.particles.ParticleOptions
//Network and commands both pass particle options as "type id + type-specific parameters"
public abstract class ParticleOptions
{
    //Type the owning particle type
    public abstract ParticleType Type { get; }
}

//SimpleParticleOption parameterless particle option, maps to the vanilla style where SimpleParticleType itself is the option
public sealed class SimpleParticleOption(SimpleParticleType type) : ParticleOptions
{
    public override ParticleType Type { get; } = type;
}

//BlockParticleOption carries a block state, maps to vanilla BlockParticleOption
public sealed class BlockParticleOption(BlockParticleType type, BlockState state) : ParticleOptions
{
    public override ParticleType Type { get; } = type;

    public BlockState State { get; } = state;
}

//ItemParticleOption carries an item stack, maps to vanilla ItemParticleOption
public sealed class ItemParticleOption(ItemParticleType type, ItemStack item) : ParticleOptions
{
    public override ParticleType Type { get; } = type;

    public ItemStack Item { get; } = item;
}

//DustParticleOptions color and scale, maps to vanilla DustParticleOptions
public sealed class DustParticleOptions(DustParticleType type, int color, float scale) : ParticleOptions
{
    //RedstoneParticleColor redstone default color, maps to vanilla REDSTONE_PARTICLE_COLOR
    public const int RedstoneParticleColor = 0xFF0000;

    //MinScale/MaxScale scale range, maps to vanilla ScalableParticleOptionsBase
    public const float MinScale = 0.01f;
    public const float MaxScale = 4.0f;

    public override ParticleType Type { get; } = type;

    public int Color { get; } = color;

    public float Scale { get; } = Math.Clamp(scale, MinScale, MaxScale);
}

//DustColorTransitionOptions two-end colors and scale, maps to vanilla DustColorTransitionOptions
public sealed class DustColorTransitionOptions(DustColorTransitionParticleType type, int fromColor, int toColor, float scale) : ParticleOptions
{
    public override ParticleType Type { get; } = type;

    public int FromColor { get; } = fromColor;

    public int ToColor { get; } = toColor;

    public float Scale { get; } = Math.Clamp(scale, DustParticleOptions.MinScale, DustParticleOptions.MaxScale);
}

//ColorParticleOption single color, maps to vanilla ColorParticleOption, covering entity_effect/tinted_leaves/flash
public sealed class ColorParticleOption(ColorParticleType type, int color) : ParticleOptions
{
    public override ParticleType Type { get; } = type;

    public int Color { get; } = color;
}

//ScalableParticleOptionsBase shared scale range for scalable particle options, maps to vanilla ScalableParticleOptionsBase
public abstract class ScalableParticleOptionsBase(float scale) : ParticleOptions
{
    public const float MinScale = 0.01f;
    public const float MaxScale = 4.0f;

    public float Scale { get; } = Math.Clamp(scale, MinScale, MaxScale);
}

//PowerParticleOption single power value, maps to vanilla PowerParticleOption, covering dragon_breath
public sealed class PowerParticleOption(PowerParticleType type, float power) : ParticleOptions
{
    //Create builds an option with the vanilla default power
    public static PowerParticleOption Create(PowerParticleType type, float power = 1.0f) => new(type, power);

    public override ParticleType Type { get; } = type;

    public float Power { get; } = power;
}

//SpellParticleOption color and power, maps to vanilla SpellParticleOption, covering effect/instant_effect
public sealed class SpellParticleOption(SpellParticleType type, int color, float power) : ParticleOptions
{
    //DefaultColor the vanilla fallback color -1, meaning the renderer picks the effect color
    public const int DefaultColor = -1;

    //Create builds an option with the vanilla defaults
    public static SpellParticleOption Create(SpellParticleType type, int color = DefaultColor, float power = 1.0f)
        => new(type, color, power);

    public override ParticleType Type { get; } = type;

    public int Color { get; } = color;

    public float Power { get; } = power;
}

//SculkChargeParticleOptions roll angle, maps to vanilla SculkChargeParticleOptions
public sealed class SculkChargeParticleOptions(SculkChargeParticleType type, float roll) : ParticleOptions
{
    public override ParticleType Type { get; } = type;

    public float Roll { get; } = roll;
}

//ShriekParticleOption delay in ticks, maps to vanilla ShriekParticleOption
public sealed class ShriekParticleOption(ShriekParticleType type, int delay) : ParticleOptions
{
    public override ParticleType Type { get; } = type;

    public int Delay { get; } = delay;
}

//VibrationParticleOption destination and arrival delay, maps to vanilla VibrationParticleOption
//The destination is never an entity source on the wire: the flying particle has to know a fixed point
public sealed class VibrationParticleOption(VibrationParticleType type, PositionSource destination, int arrivalInTicks) : ParticleOptions
{
    public override ParticleType Type { get; } = type;

    public PositionSource Destination { get; } = destination;

    public int ArrivalInTicks { get; } = arrivalInTicks;
}

//ExplosionParticleInfo particle, scaling and speed of one explosion particle entry, maps to vanilla ExplosionParticleInfo
//Explosion definitions in world generation data carry a list of these
public sealed class ExplosionParticleInfo(ParticleOptions particle, float scaling, float speed)
{
    public ParticleOptions Particle { get; } = particle;

    public float Scaling { get; } = scaling;

    public float Speed { get; } = speed;
}
