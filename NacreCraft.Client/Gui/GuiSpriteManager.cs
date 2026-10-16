using System.IO;
using System.Text.Json;
using NacreCraft.Client.Blaze3d.Systems;
using NacreCraft.Client.Blaze3d.Buffers;
using NacreCraft.Client.Blaze3d.Textures;
using NacreCraft.Client.Blaze3d.Shaders;
using NacreCraft.Client.Blaze3d.Pipeline;
using NacreCraft.Client.Blaze3d.Vertex;
using NacreCraft.Client.Blaze3d.Vulkan;
using NacreCraft.Client.Blaze3d.Platform;
using NacreCraft.Client.Blaze3d.Font;
using NacreCraft.Client.Blaze3d.Resource;
using NacreCraft.Client.Blaze3d.Audio;
using NacreCraft.Client.Blaze3d.Framegraph;
using NacreCraft.Client.Blaze3d.Preprocessor;
using NacreCraft.Client.Blaze3d.Util;
using NacreCraft.Client.Render;
using NacreCraft.Client.Render.Model;
using NacreCraft.Client.Render.Texture;
using NacreCraft.Client.Render.Texture.Atlas;
using NacreCraft.Client.Render.Item;
using NacreCraft.Client.Render.Entity;
using NacreCraft.Client.Render.Entity.State;
using NacreCraft.Client.Render.State.Gui;
using NacreCraft.Client.Gui;
using NacreCraft.Client.Gui.Render;
using NacreCraft.Client.Gui.Render.State;
using NacreCraft.Client.Gui.Render.Pip;
using NacreCraft.Client.Gui.Navigation;
using NacreCraft.Client.Gui.Layouts;
using NacreCraft.Client.Gui.Font;
using NacreCraft.Client.Gui.Font.Providers;
using NacreCraft.Client.Gui.Font.Glyphs;
using NacreCraft.Client.Model;
using NacreCraft.Client.Model.Geom;
using NacreCraft.Client.Resources.Metadata.Gui;
using NacreCraft.Registry;
using ClientPackType = NacreCraft.Resources.PackType;
using ClientResourceManager = NacreCraft.Resources.ResourceManager;

namespace NacreCraft.Client.Gui;

//GuiSpriteManager identifier → GuiSprite cache, maps to vanilla TextureAtlas guiSprites
//Sprites are read through the client ResourceManager, so any enabled resource pack can override one
//Lazily loads the PNG via GuiResourceManager.RegisterTexture and reads the same-named .mcmeta file
//LoadSprite is protected virtual for tests to override without depending on GpuDevice
public class GuiSpriteManager
{
    private readonly ClientResourceManager? _resources;
    private readonly GuiResourceManager _resourceManager;
    private readonly Dictionary<string, GuiSprite?> _cache = new();

    public GuiSpriteManager(ClientResourceManager? resources, GuiResourceManager resourceManager)
    {
        _resources = resources;
        _resourceManager = resourceManager;
    }

    //GetSprite gets a GuiSprite by identifier, lazily loading the PNG + .mcmeta if not loaded
    //Returns null when the PNG is not found; the caller handles the fallback
    //On a cache hit it returns the previous result including null results, avoiding repeated IO
    public GuiSprite? GetSprite(string identifier)
    {
        if (_cache.TryGetValue(identifier, out var cached)) return cached;
        var sprite = LoadSprite(identifier);
        _cache[identifier] = sprite;
        return sprite;
    }

    //ClearCache called by VulkanGuiApp to clear the cache on swapchain recreation
    //textureId may change, invalidating the TextureSetup held by old GuiSprites
    public void ClearCache() => _cache.Clear();

    //LoadSprite resolves the PNG and the same-named .mcmeta through the resource manager
    //identifier "minecraft:textures/gui/sprites/widget/button" → resource minecraft:textures/gui/sprites/widget/button.png
    //virtual for tests to override without real file IO and GpuDevice dependency
    protected virtual GuiSprite? LoadSprite(string identifier)
    {
        if (_resources is null) return null;
        var (ns, path) = SplitIdentifier(identifier);
        var location = Identifier.TryBuild(ns, path + ".png");
        if (location is null) return null;
        var png = _resources.GetResource(ClientPackType.ClientResources, location.Value);
        if (png is null) return null;

        int textureId;
        using (var stream = png.Open())
        {
            textureId = _resourceManager.RegisterTexture(stream, location.Value.ToString());
        }
        if (textureId == 0) return null;
        var texture = _resourceManager.ResolveTexture(textureId);
        if (texture is null || texture.Texture0 is null) return null;

        var img = texture.Texture0;
        var scaling = GuiSpriteScaling.Default;
        var metaLocation = Identifier.TryBuild(ns, path + ".png.mcmeta");
        if (metaLocation is not null)
        {
            var meta = _resources.GetResource(ClientPackType.ClientResources, metaLocation.Value);
            if (meta is not null)
            {
                try
                {
                    using var stream = meta.Open();
                    using var doc = JsonDocument.Parse(stream);
                    scaling = GuiMetadataSection.Parse(doc.RootElement.Clone());
                }
                catch
                {
                    //A parse failure falls back to the default Stretch without blocking
                }
            }
        }

        return new GuiSprite(textureId, texture, img.Width, img.Height, scaling);
    }

    //SplitIdentifier "minecraft:textures/gui/sprites/widget/button"
    //  → ("minecraft", "textures/gui/sprites/widget/button")
    //  no colon → ("minecraft", identifier), maps to the font resource accessor
    private protected static (string ns, string path) SplitIdentifier(string identifier)
    {
        var idx = identifier.IndexOf(':');
        return idx > 0
            ? (identifier.Substring(0, idx), identifier.Substring(idx + 1))
            : ("minecraft", identifier);
    }
}
