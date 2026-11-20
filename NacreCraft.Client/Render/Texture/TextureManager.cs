using NacreCraft.Logging;
using NacreCraft.Registry;
using NacreCraft.Resources;

namespace NacreCraft.Client.Render.Texture;

//TextureManager owns every named texture and loads missing ones on demand, maps to vanilla net.minecraft.client.renderer.texture.TextureManager
//NC has no ReloadableTexture yet, so loading happens when the texture is first requested rather than during a resource reload
public sealed class TextureManager : IDisposable
{
    public static readonly Identifier INTENTIONAL_MISSING_TEXTURE = Identifier.WithDefaultNamespace("");

    private readonly Dictionary<Identifier, AbstractTexture> _byPath = new();
    private readonly ResourceManager _resourceManager;
    private bool _disposed;

    public TextureManager(ResourceManager resourceManager)
    {
        _resourceManager = resourceManager;
        var (pixels, width, height) = MissingTextureAtlasSprite.GenerateMissingImage();
        Register(MissingTextureAtlasSprite.GetLocation(), new DynamicTexture("(intentionally-)Missing Texture", pixels, width, height));
    }

    //Register stores a texture object, closing the one it replaces
    public void Register(Identifier location, AbstractTexture texture)
    {
        if (_byPath.TryGetValue(location, out var previous) && !ReferenceEquals(previous, texture))
            SafeClose(location, previous);
        _byPath[location] = texture;
    }

    //RegisterForNextReload registers a not-yet-loaded texture, maps to vanilla registerForNextReload
    public void RegisterForNextReload(Identifier location) => Register(location, new SimpleTexture(location, _resourceManager));

    //RegisterAndLoad applies a reloadable texture and stores it, maps to vanilla registerAndLoad
    public void RegisterAndLoad(Identifier location, ReloadableTexture texture)
    {
        texture.Apply(texture.LoadContents(_resourceManager));
        Register(location, texture);
    }

    //Reload re-reads every reloadable texture from the resource packs
    public void Reload()
    {
        foreach (var texture in _byPath.Values.ToArray())
        {
            if (texture is not ReloadableTexture reloadable) continue;
            try
            {
                reloadable.Apply(reloadable.LoadContents(_resourceManager));
            }
            catch (Exception ex)
            {
                Log.Warning($"Failed to reload texture {reloadable.ResourceId}: {ex.Message}");
            }
        }
    }

    //GetTexture returns the texture, loading a SimpleTexture when it is not registered yet
    public AbstractTexture GetTexture(Identifier location)
    {
        if (_byPath.TryGetValue(location, out var texture)) return texture;
        var simple = new SimpleTexture(location, _resourceManager);
        Register(location, simple);
        return simple;
    }

    //Release closes and removes one texture
    public void Release(Identifier location)
    {
        if (!_byPath.Remove(location, out var texture)) return;
        SafeClose(location, texture);
    }

    //Tick reserved for animated/tickable textures; NC has none yet, maps to vanilla tick
    public void Tick()
    {
    }

    private static void SafeClose(Identifier location, AbstractTexture texture)
    {
        try
        {
            texture.Dispose();
        }
        catch (Exception ex)
        {
            Log.Warning($"Failed to close texture {location}: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        foreach (var (location, texture) in _byPath)
            SafeClose(location, texture);
        _byPath.Clear();
        _disposed = true;
    }
}
