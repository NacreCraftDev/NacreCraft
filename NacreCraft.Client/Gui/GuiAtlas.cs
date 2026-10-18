using System.IO;
using System.Text.Json;
using StbImageSharp;
using NacreCraft.Client.Blaze3d;
using NacreCraft.Client.Blaze3d.Systems;
using NacreCraft.Client.Blaze3d.Textures;
using NacreCraft.Client.Gui.Render;
using NacreCraft.Client.Render.Texture.Atlas;
using NacreCraft.Client.Resources.Metadata.Gui;
using NacreCraft.Registry;
using ClientPackType = NacreCraft.Resources.PackType;
using ClientResourceManager = NacreCraft.Resources.ResourceManager;

namespace NacreCraft.Client.Gui;

//GuiAtlas the stitched GUI sprite atlas, maps to vanilla AtlasIds.GUI + TextureAtlas
//Vanilla collects gui/sprites and mob_effect through the atlas definition, stitches them on reload and samples the
//result with getClampToEdge(NEAREST); NC stitches the same directories with the block atlas stitcher
//The atlas texture does not depend on the swapchain extent, so it survives a resize instead of being re-uploaded
public sealed class GuiAtlas : IDisposable
{
    //A GUI sprite is small, so 2048 covers the whole vanilla set with room for packs
    private const int MaxAtlasSize = 2048;
    private const string GuiSpritesPrefix = "textures/gui/sprites";
    private const string MobEffectPrefix = "textures/mob_effect";

    private readonly GpuTexture _texture;
    private readonly GpuSampler _sampler;
    private readonly TextureSetup _setup;
    private readonly GpuTexture _missingTexture;
    private readonly GpuSampler _missingSampler;
    private readonly Dictionary<string, GuiSprite> _sprites = new();

    //Count sprites stitched into the atlas, for the load log
    public int Count => _sprites.Count;
    public int Width { get; }
    public int Height { get; }
    //Missing placeholder returned for an identifier the atlas does not contain, maps to vanilla missingno
    public GuiSprite Missing { get; }

    private GuiAtlas(GpuTexture texture, GpuSampler sampler, int width, int height,
        Dictionary<string, GuiSprite> sprites, GpuTexture missingTexture, GpuSampler missingSampler, GuiSprite missing)
    {
        _texture = texture;
        _sampler = sampler;
        _setup = TextureSetup.SingleTexture(texture, sampler);
        Width = width;
        Height = height;
        _sprites = sprites;
        _missingTexture = missingTexture;
        _missingSampler = missingSampler;
        Missing = missing;
    }

    //GetSprite resolves an atlas relative id such as minecraft:widget/button
    //The legacy full file path form minecraft:textures/gui/sprites/widget/button resolves to the same sprite
    public GuiSprite? GetSprite(string identifier)
        => _sprites.TryGetValue(Normalize(identifier), out var sprite) ? sprite : null;

    //Normalize drops the .png suffix and the textures/gui/sprites/ prefix, so both id forms map to one key
    internal static string Normalize(string identifier)
    {
        var id = identifier;
        if (id.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) id = id[..^4];
        var index = id.IndexOf(GuiSpritesPrefix, StringComparison.Ordinal);
        if (index >= 0) id = id[..index] + id[(index + GuiSpritesPrefix.Length)..];
        return id;
    }

    //Build stitches every GUI sprite from the enabled packs
    //Returns null when there is nothing to stitch or the set does not fit, and the caller falls back to per-file loading
    public static GuiAtlas? Build(ClientResourceManager? resources, GpuDevice device, IGpuLogger? logger)
    {
        if (resources is null) return null;
        var entries = new List<TextureStitcher.SpriteInput>();
        var scalings = new Dictionary<string, GuiSpriteScaling>();
        Collect(resources, GuiSpritesPrefix, string.Empty, entries, scalings, logger);
        Collect(resources, MobEffectPrefix, "mob_effect/", entries, scalings, logger);
        if (entries.Count == 0) return null;

        var stitcher = new TextureStitcher(MaxAtlasSize);
        if (!stitcher.Stitch(entries))
        {
            logger?.Warning($"GUI atlas does not fit {entries.Count} sprites within {MaxAtlasSize}px");
            return null;
        }
        var width = stitcher.AtlasWidth;
        var height = stitcher.AtlasHeight;
        //Composed on the CPU and uploaded once; uploading region by region costs one submit-and-wait per sprite
        var pixels = new byte[width * height * 4];
        foreach (var placed in stitcher.Placed)
        {
            if (placed.Pixels is null) continue;
            for (var row = 0; row < placed.Height; row++)
            {
                var src = row * placed.Width * 4;
                var dst = ((placed.AtlasY + row) * width + placed.AtlasX) * 4;
                Buffer.BlockCopy(placed.Pixels, src, pixels, dst, placed.Width * 4);
            }
        }

        var texture = device.CreateTexture(null, GpuTexture.UsageTextureBinding, GpuFormat.Rgba8Unorm, width, height, 1, 1);
        texture.Upload(pixels);
        //GUI_TEXTURED samples the atlas with NEAREST on both filters, maps to TextureAtlas.upload
        var sampler = device.CreateSampler(AddressMode.ClampToEdge, AddressMode.ClampToEdge, FilterMode.Nearest, FilterMode.Nearest, 1, null);
        var sprites = new Dictionary<string, GuiSprite>(entries.Count);
        foreach (var placed in stitcher.Placed)
        {
            var scaling = scalings.TryGetValue(placed.Name, out var parsed) ? parsed : GuiSpriteScaling.Default;
            sprites[placed.Name] = new GuiSprite(placed.AtlasX, placed.AtlasY, width, height,
                placed.Width, placed.Height, TextureSetup.SingleTexture(texture, sampler), scaling);
        }
        logger?.Info($"GUI atlas {width}x{height} sprites={sprites.Count}");
        var (missingTexture, missingSampler, missing) = CreateMissing(device);
        return new GuiAtlas(texture, sampler, width, height, sprites, missingTexture, missingSampler, missing);
    }

    //CreateMissing builds the placeholder drawn for an identifier the atlas does not contain
    //maps to vanilla missingno, a magenta/black checker; it lives in its own small texture so it exists even when the
    //atlas itself failed to build
    private static (GpuTexture Texture, GpuSampler Sampler, GuiSprite Sprite) CreateMissing(GpuDevice device)
    {
        const int size = 16;
        var pixels = new byte[size * size * 4];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var light = (((x >> 1) + (y >> 1)) & 1) == 0;
                var offset = ((y * size) + x) * 4;
                pixels[offset] = light ? (byte)255 : (byte)0;
                pixels[offset + 1] = 0;
                pixels[offset + 2] = light ? (byte)255 : (byte)0;
                pixels[offset + 3] = 255;
            }
        }
        var texture = device.CreateTexture(null, GpuTexture.UsageTextureBinding, GpuFormat.Rgba8Unorm, size, size, 1, 1);
        texture.Upload(pixels);
        var sampler = device.CreateSampler(AddressMode.ClampToEdge, AddressMode.ClampToEdge, FilterMode.Nearest, FilterMode.Nearest, 1, null);
        var sprite = new GuiSprite(0, TextureSetup.SingleTexture(texture, sampler), size, size, GuiSpriteScaling.Default);
        return (texture, sampler, sprite);
    }

    //Collect reads one sprite directory across every namespace, decodes the PNG and parses its .mcmeta scaling
    private static void Collect(ClientResourceManager resources, string prefix, string idPrefix,
        List<TextureStitcher.SpriteInput> entries, Dictionary<string, GuiSpriteScaling> scalings, IGpuLogger? logger)
    {
        foreach (var ns in resources.GetNamespaces(ClientPackType.ClientResources))
        {
            foreach (var id in resources.ListResourceIds(ClientPackType.ClientResources, ns, prefix))
            {
                var path = id.Path;
                if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) continue;
                var resource = resources.GetResource(ClientPackType.ClientResources, id);
                if (resource is null) continue;
                ImageResult image;
                try
                {
                    using var stream = resource.Open();
                    using var buffer = new MemoryStream();
                    stream.CopyTo(buffer);
                    image = ImageResult.FromMemory(buffer.ToArray(), ColorComponents.RedGreenBlueAlpha);
                }
                catch (Exception ex)
                {
                    logger?.Warning($"GUI sprite decode failed {id} {ex.Message}");
                    continue;
                }
                if (image.Width <= 0 || image.Height <= 0) continue;
                //The sprite id is the path relative to the sprite directory, maps to vanilla SpriteSources.DirectoryLister
                var relative = path.Substring(prefix.Length).TrimStart('/');
                relative = relative[..^4];
                var spriteId = ns + ":" + idPrefix + relative;
                entries.Add(new TextureStitcher.SpriteInput(spriteId, image.Width, image.Height, image.Data));
                scalings[spriteId] = ReadScaling(resources, id, logger);
            }
        }
    }

    //ReadScaling parses the sprite's .png.mcmeta gui.scaling section, defaulting to Stretch when absent
    private static GuiSpriteScaling ReadScaling(ClientResourceManager resources, Identifier pngId, IGpuLogger? logger)
    {
        var metaLocation = Identifier.TryBuild(pngId.Namespace, pngId.Path + ".mcmeta");
        if (metaLocation is null) return GuiSpriteScaling.Default;
        var meta = resources.GetResource(ClientPackType.ClientResources, metaLocation.Value);
        if (meta is null) return GuiSpriteScaling.Default;
        try
        {
            using var stream = meta.Open();
            using var doc = JsonDocument.Parse(stream);
            return GuiMetadataSection.Parse(doc.RootElement.Clone());
        }
        catch (Exception ex)
        {
            logger?.Warning($"GUI sprite metadata parse failed {metaLocation.Value} {ex.Message}");
            return GuiSpriteScaling.Default;
        }
    }

    public void Dispose()
    {
        _texture.Dispose();
        _sampler.Dispose();
        _missingTexture.Dispose();
        _missingSampler.Dispose();
    }
}
