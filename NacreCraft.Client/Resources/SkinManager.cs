using NacreCraft.Client.Render.Texture;
using NacreCraft.Game.World.Entity;
using NacreCraft.Network.Protocol.Login;

namespace NacreCraft.Client.Resources;

//SkinManager resolves player skins, maps to vanilla net.minecraft.client.resources.SkinManager
//NC has no Mojang skin service yet, so every profile resolves to its deterministic DefaultPlayerSkin; the TextureManager
//is held so a remote skin fetch can register the downloaded texture without changing this API
public sealed class SkinManager : IDisposable
{
    private readonly TextureManager _textureManager;

    public SkinManager(TextureManager textureManager) => _textureManager = textureManager;

    //Get returns the skin for a profile, currently always the default skin
    public Task<PlayerSkin?> Get(GameProfile profile) => Task.FromResult<PlayerSkin?>(DefaultPlayerSkin.Get(profile));

    public Task<PlayerSkin?> Get(Guid profileId) => Task.FromResult<PlayerSkin?>(DefaultPlayerSkin.Get(profileId));

    public void Dispose()
    {
    }
}
