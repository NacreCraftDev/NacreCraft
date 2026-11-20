using NacreCraft.Game.World.Entity;
using NacreCraft.Network.Protocol.Login;
using NacreCraft.Registry;

namespace NacreCraft.Client.Resources;

//DefaultPlayerSkin the built-in player skins used when no profile skin is available, maps to vanilla net.minecraft.client.resources.DefaultPlayerSkin
//The skin body location is the TextureManager-relative path, so textures/<location>.png is the PNG on disk
public static class DefaultPlayerSkin
{
    private static readonly PlayerSkin[] DefaultSkins =
    {
        Create("entity/player/slim/alex", PlayerModelType.SLIM),
        Create("entity/player/slim/ari", PlayerModelType.SLIM),
        Create("entity/player/slim/efe", PlayerModelType.SLIM),
        Create("entity/player/slim/kai", PlayerModelType.SLIM),
        Create("entity/player/slim/makena", PlayerModelType.SLIM),
        Create("entity/player/slim/noor", PlayerModelType.SLIM),
        Create("entity/player/slim/steve", PlayerModelType.SLIM),
        Create("entity/player/slim/sunny", PlayerModelType.SLIM),
        Create("entity/player/slim/zuri", PlayerModelType.SLIM),
        Create("entity/player/wide/alex", PlayerModelType.WIDE),
        Create("entity/player/wide/ari", PlayerModelType.WIDE),
        Create("entity/player/wide/efe", PlayerModelType.WIDE),
        Create("entity/player/wide/kai", PlayerModelType.WIDE),
        Create("entity/player/wide/makena", PlayerModelType.WIDE),
        Create("entity/player/wide/noor", PlayerModelType.WIDE),
        Create("entity/player/wide/steve", PlayerModelType.WIDE),
        Create("entity/player/wide/sunny", PlayerModelType.WIDE),
        Create("entity/player/wide/zuri", PlayerModelType.WIDE)
    };

    public static Identifier GetDefaultTexture() => GetDefaultSkin().Body.Location;

    public static PlayerSkin GetDefaultSkin() => DefaultSkins[6];

    public static PlayerSkin Get(Guid profileId)
        => DefaultSkins[(int)((uint)profileId.GetHashCode() % DefaultSkins.Length)];

    public static PlayerSkin Get(GameProfile profile) => Get(profile.Id);

    private static PlayerSkin Create(string body, PlayerModelType model)
        => new(new PlayerSkin.SkinTexture(Identifier.WithDefaultNamespace(body)), null, null, model, true);
}
