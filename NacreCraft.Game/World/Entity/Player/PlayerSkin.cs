using NacreCraft.Registry;

namespace NacreCraft.Game.World.Entity;

//PlayerSkin the textures and model a player renders with, maps to vanilla net.minecraft.world.entity.player.PlayerSkin
//NC has no ClientAsset yet, so a texture is identified by its resource location plus the optional path inside the texture atlas;
//the server only cares about the model and the secure flag, the renderer resolves the rest
public sealed record PlayerSkin(
    PlayerSkin.SkinTexture Body,
    PlayerSkin.SkinTexture? Cape,
    PlayerSkin.SkinTexture? Elytra,
    PlayerModelType Model,
    bool Secure)
{
    //Insecure marks a skin whose textures did not come from the services session, maps to vanilla insecure
    public static PlayerSkin Insecure(SkinTexture body, SkinTexture? cape, SkinTexture? elytra, PlayerModelType model)
        => new(body, cape, elytra, model, false);

    //With applies the non-empty parts of a patch, maps to vanilla with(Patch)
    public PlayerSkin With(Patch patch)
    {
        if (patch == Patch.Empty) return this;
        return Insecure(patch.Body ?? Body, patch.Cape ?? Cape, patch.Elytra ?? Elytra, patch.Model ?? Model);
    }

    //SkinTexture one texture reference, maps to vanilla ClientAsset.Texture as used by PlayerSkin
    public sealed record SkinTexture(Identifier Location, string? TexturePath = null)
    {
        public static SkinTexture Of(Identifier location) => new(location);
    }

    //Patch a partial skin update, maps to vanilla PlayerSkin.Patch
    public sealed record Patch(SkinTexture? Body, SkinTexture? Cape, SkinTexture? Elytra, PlayerModelType? Model)
    {
        public static readonly Patch Empty = new(null, null, null, null);

        //Create returns Empty when every part is missing, maps to vanilla Patch.create
        public static Patch Create(SkinTexture? body, SkinTexture? cape, SkinTexture? elytra, PlayerModelType? model)
            => body is null && cape is null && elytra is null && model is null
                ? Empty
                : new Patch(body, cape, elytra, model);
    }
}
