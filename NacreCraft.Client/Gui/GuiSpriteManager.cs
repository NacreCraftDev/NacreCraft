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
using NacreCraft.Logging;
using NacreCraft.Registry;
using ClientPackType = NacreCraft.Resources.PackType;
using ClientResourceManager = NacreCraft.Resources.ResourceManager;

namespace NacreCraft.Client.Gui;

//GuiSpriteManager identifier → GuiSprite cache, maps to vanilla TextureAtlas guiSprites
//Sprites come from the stitched GUI atlas first; a miss falls back to loading the standalone PNG through the client
//ResourceManager, so any enabled resource pack can still override one
//LoadSprite is protected virtual for tests to override without depending on GpuDevice
public class GuiSpriteManager
{
    private readonly ClientResourceManager? _resources;
    private readonly GuiResourceManager _resourceManager;
    private readonly Dictionary<string, GuiSprite?> _cache = new();
    //_missingReported identifiers already reported, so a missing sprite warns once instead of every frame
    private readonly HashSet<string> _missingReported = new();

    //Atlas the stitched GUI sprite atlas; nullable when there is no client resource manager
    public GuiAtlas? Atlas { get; set; }

    public GuiSpriteManager(ClientResourceManager? resources, GuiResourceManager resourceManager, GuiAtlas? atlas)
    {
        _resources = resources;
        _resourceManager = resourceManager;
        Atlas = atlas;
    }

    //GetSprite gets a GuiSprite by identifier: the atlas is consulted first, then the standalone PNG + .mcmeta
    //A miss is logged once and resolved to the missing placeholder, maps to vanilla TextureAtlas.getSprite returning
    //missingno; drawing nothing is what makes a missing texture invisible instead of diagnosable
    //On a cache hit it returns the previous result, avoiding repeated IO
    public GuiSprite? GetSprite(string identifier)
    {
        if (Atlas?.GetSprite(identifier) is { } atlasSprite) return atlasSprite;
        if (_cache.TryGetValue(identifier, out var cached))
            return cached ?? ReportMissing(identifier);
        var sprite = LoadSprite(identifier);
        _cache[identifier] = sprite;
        return sprite ?? ReportMissing(identifier);
    }

    //ReportMissing logs the identifier once and returns the placeholder, or null when the atlas is unavailable
    private GuiSprite? ReportMissing(string identifier)
    {
        if (_missingReported.Add(identifier))
            Log.Warning($"Missing GUI sprite {identifier}");
        return Atlas?.Missing;
    }

    //ClearCache called by VulkanGuiApp to clear the cache on swapchain recreation or after a resource reload
    //textureId may change, invalidating the TextureSetup held by old GuiSprites; a reload can also add a sprite that
    //was missing before, so the reported set is cleared as well
    public void ClearCache()
    {
        _cache.Clear();
        _missingReported.Clear();
    }

    //LoadSprite resolves the PNG and the same-named .mcmeta through the resource manager
    //identifier "minecraft:textures/gui/sprites/widget/button" → resource minecraft:textures/gui/sprites/widget/button.png
    //The atlas-relative form minecraft:widget/button is expanded to the same path, so both id forms reach a pack file
    //virtual for tests to override without real file IO and GpuDevice dependency
    protected virtual GuiSprite? LoadSprite(string identifier)
    {
        if (_resources is null) return null;
        var (ns, path) = SplitIdentifier(identifier);
        if (!path.StartsWith("textures/", StringComparison.Ordinal))
            path = path.StartsWith("mob_effect/", StringComparison.Ordinal) ? "textures/" + path : "textures/gui/sprites/" + path;
        var location = Identifier.TryBuild(ns, path + ".png");
        if (location is null) return null;
        var png = _resources.GetResource(ClientPackType.ClientResources, location.Value);
        if (png is null) return null;

        int textureId;
        using (var stream = png.Open())
        {
            //Sprites are sampled NEAREST like the vanilla GUI atlas, otherwise guiScale magnification blurs the
            //1px borders that every widget texture is built from
            textureId = _resourceManager.RegisterTexture(stream, location.Value.ToString(), nearest: true);
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
