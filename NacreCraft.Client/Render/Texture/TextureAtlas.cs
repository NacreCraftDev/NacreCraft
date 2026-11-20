using NacreCraft.Client.Blaze3d.Textures;
using NacreCraft.Registry;

namespace NacreCraft.Client.Render.Texture;

//TextureAtlas a named collection of sprites sharing one GpuTexture, maps to vanilla net.minecraft.client.renderer.texture.TextureAtlas
//NC builds atlases from TextureAtlasSprite lists; sprite lookup is by Identifier path
public sealed class TextureAtlas
{
    private readonly Dictionary<Identifier, TextureAtlasSprite> _sprites = new();

    public Identifier Location { get; }
    public TextureAtlasSprite MissingSprite { get; }
    private GpuTextureView? TextureView { get; }

    public TextureAtlas(Identifier location, GpuTextureView? textureView, TextureAtlasSprite missingSprite,
        IEnumerable<TextureAtlasSprite> sprites)
    {
        Location = location;
        TextureView = textureView;
        MissingSprite = missingSprite;
        foreach (var sprite in sprites)
            _sprites[Identifier.Parse(sprite.Name)] = sprite;
    }

    //GetSprite returns the sprite for the id, or the missing sprite when absent
    public TextureAtlasSprite GetSprite(Identifier id)
        => _sprites.TryGetValue(id, out var sprite) ? sprite : MissingSprite;

    //MissingSprite returns the placeholder sprite
    public TextureAtlasSprite MissingSpriteValue => MissingSprite;

    public GpuTextureView GetTextureView()
        => TextureView ?? throw new InvalidOperationException("Texture atlas has no GPU texture view");

    public bool HasTextureView => TextureView is not null;
}
