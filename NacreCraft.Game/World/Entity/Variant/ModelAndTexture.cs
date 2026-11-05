using NacreCraft.Codec;
using NacreCraft.Network;

namespace NacreCraft.Game.World.Entity.Variant;

//ModelAndTexture a variant's model plus texture, maps to vanilla net.minecraft.world.entity.variant.ModelAndTexture
public sealed record ModelAndTexture<T>(T Model, ClientAsset.ResourceTexture Asset)
{
    public ModelAndTexture(T model, NacreCraft.Registry.Identifier assetId)
        : this(model, new ClientAsset.ResourceTexture(assetId))
    {
    }

    public static MapCodec<ModelAndTexture<T>> Codec(Codec<T> modelCodec, T defaultModel)
        => RecordCodecBuilder.Of2(
            modelCodec.OptionalFieldOf("model", defaultModel).ForGetter((ModelAndTexture<T> value) => value.Model),
            ClientAsset.ResourceTexture.DEFAULT_FIELD_CODEC.ForGetter((ModelAndTexture<T> value) => value.Asset),
            (model, asset) => new ModelAndTexture<T>(model, asset));

    public static StreamCodec<RegistryFriendlyByteBuf, ModelAndTexture<T>> StreamCodecOf(
        StreamCodec<RegistryFriendlyByteBuf, T> modelCodec)
        => StreamCodecs.Composite(modelCodec, (ModelAndTexture<T> value) => value.Model,
            ClientAsset.ResourceTexture.STREAM_CODEC, (ModelAndTexture<T> value) => value.Asset,
            (model, asset) => new ModelAndTexture<T>(model, asset));
}
