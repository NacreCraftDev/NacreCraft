using NacreCraft.Codec;
using NacreCraft.Network;
using NacreCraft.Registry;
using NacreCraft.Registry.Codec;

namespace NacreCraft.Game.World.Entity.Variant;

//ClientAsset a client-side asset reference, maps to vanilla net.minecraft.core.ClientAsset
//Vanilla declares id()/texturePath() methods; C# records expose the same data as properties
public interface ClientAsset
{
    Identifier Id { get; }

    //Texture an asset with a texture path, maps to vanilla ClientAsset.Texture
    public interface Texture : ClientAsset
    {
        Identifier TexturePath { get; }
    }

    //ResourceTexture a texture shipped inside the pack, maps to vanilla ClientAsset.ResourceTexture
    public sealed record ResourceTexture(Identifier Id, Identifier TexturePath) : Texture
    {
        public static readonly Codec<ResourceTexture> CODEC = IdentifierCodec.Instance.ComapFlatMap(
            id => DataResult<ResourceTexture>.Success(new ResourceTexture(id)),
            texture => texture.Id);

        public static readonly MapCodec<ResourceTexture> DEFAULT_FIELD_CODEC = CODEC.FieldOf("asset_id");

        public static readonly StreamCodec<FriendlyByteBuf, ResourceTexture> STREAM_CODEC = new ResourceTextureStreamCodec();

        public ResourceTexture(Identifier texture) : this(texture, texture.WithPath(path => "textures/" + path + ".png"))
        {
        }
    }
}

//ResourceTextureStreamCodec encodes a resource texture as its identifier, maps to vanilla ResourceTexture.STREAM_CODEC
internal sealed class ResourceTextureStreamCodec : StreamCodec<FriendlyByteBuf, ClientAsset.ResourceTexture>
{
    public ClientAsset.ResourceTexture Decode(FriendlyByteBuf buf) => new(buf.ReadIdentifier());

    public void Encode(FriendlyByteBuf buf, ClientAsset.ResourceTexture value) => buf.WriteIdentifier(value.Id);
}
