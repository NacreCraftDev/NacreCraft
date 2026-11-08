using NacreCraft.Codec;
using NacreCraft.Game.World.Entity;
using NacreCraft.Game.World.Items;
using NacreCraft.Network;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Items.ConsumeEffects;

//TeleportRandomlyConsumeEffect teleports the consumer to a random nearby position, maps to vanilla TeleportRandomlyConsumeEffect
//Used by chorus fruit; sixteen attempts are made before giving up, and every successful attempt resets fall distance and plays the teleport sound
public sealed class TeleportRandomlyConsumeEffect : ConsumeEffect
{
    //DefaultDiameter the vanilla default spread, maps to DEFAULT_DIAMETER
    public const float DefaultDiameter = 16f;

    //Codec persistence codec, an optional positive diameter, maps to vanilla CODEC
    public static readonly Codec<TeleportRandomlyConsumeEffect> Codec = RecordCodecBuilder.Of1(
        Codecs.Float.OptionalFieldOf("diameter", DefaultDiameter)
            .ForGetter((TeleportRandomlyConsumeEffect effect) => effect.Diameter),
        diameter => new TeleportRandomlyConsumeEffect(diameter));

    //StreamCodec network codec, the diameter float, maps to vanilla STREAM_CODEC
    public static readonly StreamCodec<RegistryFriendlyByteBuf, TeleportRandomlyConsumeEffect> StreamCodec =
        ByteBufCodecs.Float.Map(diameter => new TeleportRandomlyConsumeEffect(diameter), effect => effect.Diameter);

    public static readonly ConsumeEffectType<TeleportRandomlyConsumeEffect> EffectType =
        ConsumeEffectType.Register(Identifier.WithDefaultNamespace("teleport_randomly"),
            new SimpleConsumeEffectType<TeleportRandomlyConsumeEffect>(
                Identifier.WithDefaultNamespace("teleport_randomly"), Codec, StreamCodec));

    public TeleportRandomlyConsumeEffect() : this(DefaultDiameter)
    {
    }

    public TeleportRandomlyConsumeEffect(float diameter) => Diameter = diameter;

    //Diameter the size of the random area around the consumer
    public float Diameter { get; }

    public override ConsumeEffectType Type => EffectType;

    //Apply tries up to sixteen random positions and stops at the first successful teleport, maps to vanilla apply
    //The y range is clamped to the dimension build height; the fox variant uses the same chorus fruit sound because the animal entity is not ported
    public override bool Apply(ServerLevel level, ItemStack stack, LivingEntity user)
    {
        var teleported = false;
        for (var attempt = 0; attempt < 16 && !teleported; attempt++)
        {
            var xx = user.Pos.X + (user.GetRandom().NextDouble() - 0.5) * Diameter;
            var yy = Math.Clamp(user.Pos.Y + (user.GetRandom().NextDouble() - 0.5) * Diameter,
                level.MinBuildHeight, level.MaxBuildHeight - 1);
            var zz = user.Pos.Z + (user.GetRandom().NextDouble() - 0.5) * Diameter;
            var oldPos = user.Pos;
            if (!user.RandomTeleport(xx, yy, zz, true)) continue;
            //A successful teleport posts the vanilla teleport game event at the old position
            level.GameEvent(null, GameEvent.Teleport, oldPos);
            level.PlaySound(SoundEvents.ChorusFruitTeleport, SoundSource.Players, user.BlockPosition(), 1f, 1f);
            user.ResetFallDistance();
            teleported = true;
        }
        return teleported;
    }

    public override string ToString() => $"TeleportRandomlyConsumeEffect[diameter={Diameter}]";
}
