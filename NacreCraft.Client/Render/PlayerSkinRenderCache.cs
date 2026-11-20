using NacreCraft.Client.Gui.Font;
using NacreCraft.Client.Render.Rendertype;
using NacreCraft.Client.Render.Texture;
using NacreCraft.Client.Resources;
using NacreCraft.Game.World.Entity;
using NacreCraft.Game.World.Items.Component;
using NacreCraft.Network.Chat;
using NacreCraft.Network.Protocol.Login;

namespace NacreCraft.Client.Render;

//PlayerSkinRenderCache caches the render data of player skins, maps to vanilla net.minecraft.client.renderer.PlayerSkinRenderCache
//NC resolves skins through SkinManager (default-only today); each entry lazily creates its RenderType, texture view and
//glyph render types so a text glyph and the skull renderer share the same GPU resources
public sealed class PlayerSkinRenderCache : IDisposable
{
    public static readonly TimeSpan CACHE_DURATION = TimeSpan.FromMinutes(5);

    private readonly TextureManager _textureManager;
    private readonly SkinManager _skinManager;
    private readonly Dictionary<(Guid Id, string Name), RenderInfo> _cache = new();
    private readonly object _lock = new();
    private bool _disposed;

    public PlayerSkinRenderCache(TextureManager textureManager, SkinManager skinManager)
    {
        _textureManager = textureManager;
        _skinManager = skinManager;
    }

    //GetOrDefault returns the cached render info, resolving the default skin on a miss
    public RenderInfo GetOrDefault(GameProfile profile)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue((profile.Id, profile.Name), out var cached)) return cached;
            var skin = _skinManager.Get(profile).GetAwaiter().GetResult() ?? DefaultPlayerSkin.Get(profile);
            var info = new RenderInfo(this, profile, skin);
            _cache[(profile.Id, profile.Name)] = info;
            return info;
        }
    }

    public RenderInfo GetOrDefault(ResolvableProfile profile)
        => GetOrDefault(profile.ToGameProfile());

    public RenderInfo GetOrDefault(FontDescription.PlayerSprite sprite)
        => GetOrDefault(new GameProfile(sprite.Id, sprite.Name));

    //CreateLookup returns a supplier the glyph reads on every render, matching vanilla's delayed texture resolution
    public Func<RenderInfo> CreateLookup(GameProfile profile) => () => GetOrDefault(profile);

    public Func<RenderInfo> CreateLookup(FontDescription.PlayerSprite sprite) => () => GetOrDefault(sprite);

    public void Dispose()
    {
        if (_disposed) return;
        _cache.Clear();
        _disposed = true;
    }

    //RenderInfo one skin's lazily resolved render resources, maps to vanilla PlayerSkinRenderCache.RenderInfo
    public sealed class RenderInfo
    {
        private readonly PlayerSkinRenderCache _owner;
        private RenderType? _renderType;
        private Blaze3d.Textures.GpuTextureView? _textureView;
        private GlyphRenderTypes? _glyphRenderTypes;

        internal RenderInfo(PlayerSkinRenderCache owner, GameProfile profile, PlayerSkin skin)
        {
            _owner = owner;
            GameProfile = profile;
            PlayerSkin = skin;
        }

        public GameProfile GameProfile { get; }

        public PlayerSkin PlayerSkin { get; }

        public RenderType RenderType()
            => _renderType ??= NacreCraft.Client.Render.Rendertype.RenderTypes.EntityCutout(PlayerSkin.Body.Location);

        public Blaze3d.Textures.GpuTextureView TextureView()
            => _textureView ??= _owner._textureManager.GetTexture(PlayerSkin.Body.Location).GetTextureView();

        public GlyphRenderTypes GetGlyphRenderTypes()
            => _glyphRenderTypes ??= GlyphRenderTypes.CreateForColorTexture(PlayerSkin.Body.Location);
    }
}
