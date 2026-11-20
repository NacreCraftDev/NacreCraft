using NacreCraft.Codec;
using NacreCraft.Network;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using LevelType = NacreCraft.Registry.Level;

namespace NacreCraft.Game.World.Items.Component;

//LodestoneTracker the lodestone a compass points at, maps to vanilla net.minecraft.world.item.component.LodestoneTracker
//Dimension keys are serialized as identifiers; validation of a still-existing lodestone waits for the block entity lookup
public sealed record LodestoneTracker(Optional<GlobalPos> Target, bool Tracked)
{
    private static readonly Codec<ResourceKey<LevelType>> DimensionCodec =
        ItemComponentCodecs.IdentifierCodec.Xmap(
            id => ResourceKey<LevelType>.Create(Registries.DIMENSION, id),
            key => key.Identifier);

    private static readonly Codec<GlobalPos> GlobalPosCodec = RecordCodecBuilder.Of2(
        DimensionCodec.FieldOf("dimension").ForGetter((GlobalPos value)
            => value.DimensionKey as ResourceKey<LevelType>
               ?? ResourceKey<LevelType>.Create(Registries.DIMENSION, Identifier.WithDefaultNamespace("overworld"))),
        RecordCodecBuilder.Of3(
            Codecs.Int.FieldOf("x").ForGetter((BlockPos pos) => pos.X),
            Codecs.Int.FieldOf("y").ForGetter((BlockPos pos) => pos.Y),
            Codecs.Int.FieldOf("z").ForGetter((BlockPos pos) => pos.Z),
            (x, y, z) => new BlockPos(x, y, z)).FieldOf("pos").ForGetter((GlobalPos value) => value.Pos),
        (dimension, pos) => GlobalPos.Of(dimension, pos));

    public static readonly Codec<LodestoneTracker> Codec = RecordCodecBuilder.Of2(
        GlobalPosCodec.OptionalFieldOf("target").ForGetter((LodestoneTracker value) => value.Target),
        Codecs.Bool.OptionalFieldOf("tracked", true).ForGetter((LodestoneTracker value) => value.Tracked),
        (target, tracked) => new LodestoneTracker(target, tracked));

    public static readonly StreamCodec<FriendlyByteBuf, LodestoneTracker> StreamCodec = new LodestoneTrackerStreamCodec();

    //Tick verifies the target still exists in vanilla; NC has no lodestone block entity, so the tracker is returned unchanged
    public LodestoneTracker Tick(NacreCraft.Storage.ServerLevel level) => this;

    private sealed class LodestoneTrackerStreamCodec : StreamCodec<FriendlyByteBuf, LodestoneTracker>
    {
        public LodestoneTracker Decode(FriendlyByteBuf buf)
        {
            var hasTarget = buf.ReadBoolean();
            var target = hasTarget
                ? Optional<GlobalPos>.Of(GlobalPos.Of(
                    ResourceKey<LevelType>.Create(Registries.DIMENSION, buf.ReadIdentifier()), buf.ReadBlockPos()))
                : Optional<GlobalPos>.Empty();
            return new LodestoneTracker(target, buf.ReadBoolean());
        }

        public void Encode(FriendlyByteBuf buf, LodestoneTracker value)
        {
            buf.WriteBoolean(value.Target.IsPresent);
            if (value.Target.IsPresent)
            {
                var target = value.Target.Get();
                var dimension = target.DimensionKey as ResourceKey<LevelType>;
                buf.WriteIdentifier(dimension?.Identifier ?? Identifier.WithDefaultNamespace("overworld"));
                buf.WriteBlockPos(target.Pos);
            }
            buf.WriteBoolean(value.Tracked);
        }
    }
}
