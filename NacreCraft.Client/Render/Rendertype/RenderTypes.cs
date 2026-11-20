using System.Collections.Concurrent;
using NacreCraft.Client.Blaze3d.Pipeline;
using NacreCraft.Client.Blaze3d.Systems;
using NacreCraft.Client.Blaze3d.Textures;
using NacreCraft.Client.Render.Item;
using NacreCraft.Registry;

namespace NacreCraft.Client.Render.Rendertype;

//RenderTypes the named render types used across the client, maps to vanilla net.minecraft.client.renderer.rendertype.RenderTypes
//Texture-parameterised types are memoized so the same texture returns the same RenderType instance and can be batched
public static class RenderTypes
{
    private static readonly Identifier TextureAtlasLocation = Identifier.WithDefaultNamespace("textures/atlas/blocks.png");

    //BlockAtlasLocation the identifier the world block atlas is registered under, used by entity/item render types
    public static Identifier BlockAtlasLocation => TextureAtlasLocation;
    private static readonly Identifier EnchantedGlintArmor = Identifier.WithDefaultNamespace("textures/misc/enchanted_glint_armor.png");
    private static readonly Identifier EnchantedGlintItem = Identifier.WithDefaultNamespace("textures/misc/enchanted_glint_item.png");
    private static readonly Identifier EndSkyLocation = Identifier.WithDefaultNamespace("textures/environment/end_sky.png");
    private static readonly Identifier EndPortalLocation = Identifier.WithDefaultNamespace("textures/entity/end_portal/end_portal.png");

    private static readonly Func<Identifier, bool, RenderType> OutlineFactory = Memoize<Identifier, bool, RenderType>((texture, cullState) =>
        RenderType.Create("outline", RenderSetup.Builder(cullState ? RenderTypePipelines.OUTLINE_CULL : RenderTypePipelines.OUTLINE_NO_CULL)
            .WithTexture("Sampler0", texture)
            .SetOutputTarget(OutputTarget.OUTLINE_TARGET)
            .SetOutline(RenderSetup.OutlineProperty.IsOutline)
            .CreateRenderSetup()));

    private static readonly RenderType SOLID_MOVING_BLOCK = RenderType.Create("solid_moving_block", CreateMovingBlockSetup(RenderTypePipelines.SOLID_BLOCK, false));
    private static readonly RenderType CUTOUT_MOVING_BLOCK = RenderType.Create("cutout_moving_block", CreateMovingBlockSetup(RenderTypePipelines.CUTOUT_BLOCK, false));
    private static readonly RenderType TRANSLUCENT_MOVING_BLOCK = RenderType.Create("translucent_moving_block", CreateMovingBlockSetup(RenderTypePipelines.TRANSLUCENT_BLOCK, true));

    private static readonly Func<Identifier, RenderType> ArmorCutoutNoCullType = Memoize<Identifier, RenderType>(texture => RenderType.Create("armor_cutout_no_cull",
        RenderSetup.Builder(RenderTypePipelines.ARMOR_CUTOUT_NO_CULL).WithTexture("Sampler0", texture).UseLightmap().UseOverlay()
            .SetLayeringTransform(LayeringTransform.VIEW_OFFSET_Z_LAYERING).AffectsCrumbling()
            .SetOutline(RenderSetup.OutlineProperty.AffectsOutline).CreateRenderSetup()));

    private static readonly Func<Identifier, RenderType> ArmorTranslucentType = Memoize<Identifier, RenderType>(texture => RenderType.Create("armor_translucent",
        RenderSetup.Builder(RenderTypePipelines.ARMOR_TRANSLUCENT).WithTexture("Sampler0", texture).UseLightmap().UseOverlay()
            .SetLayeringTransform(LayeringTransform.VIEW_OFFSET_Z_LAYERING).AffectsCrumbling().SortOnUpload()
            .SetOutline(RenderSetup.OutlineProperty.AffectsOutline).CreateRenderSetup()));

    private static readonly Func<Identifier, RenderType> EntitySolidType = Memoize<Identifier, RenderType>(texture => RenderType.Create("entity_solid",
        RenderSetup.Builder(RenderTypePipelines.ENTITY_SOLID).WithTexture("Sampler0", texture).UseLightmap().UseOverlay()
            .AffectsCrumbling().SetOutline(RenderSetup.OutlineProperty.AffectsOutline).CreateRenderSetup()));

    private static readonly Func<Identifier, RenderType> EntitySolidZOffsetForwardType = Memoize<Identifier, RenderType>(texture => RenderType.Create("entity_solid_z_offset_forward",
        RenderSetup.Builder(RenderTypePipelines.ENTITY_SOLID_Z_OFFSET_FORWARD).WithTexture("Sampler0", texture).UseLightmap().UseOverlay()
            .SetLayeringTransform(LayeringTransform.VIEW_OFFSET_Z_LAYERING_FORWARD).AffectsCrumbling()
            .SetOutline(RenderSetup.OutlineProperty.AffectsOutline).CreateRenderSetup()));

    private static readonly Func<Identifier, RenderType> EntityCutoutCullType = Memoize<Identifier, RenderType>(texture => RenderType.Create("entity_cutout_cull",
        RenderSetup.Builder(RenderTypePipelines.ENTITY_CUTOUT_CULL).WithTexture("Sampler0", texture).UseLightmap().UseOverlay()
            .AffectsCrumbling().SetOutline(RenderSetup.OutlineProperty.AffectsOutline).CreateRenderSetup()));

    private static readonly Func<Identifier, bool, RenderType> EntityCutoutType = Memoize<Identifier, bool, RenderType>((texture, affectsOutline) => RenderType.Create("entity_cutout",
        RenderSetup.Builder(RenderTypePipelines.ENTITY_CUTOUT).WithTexture("Sampler0", texture).UseLightmap().UseOverlay()
            .AffectsCrumbling()
            .SetOutline(affectsOutline ? RenderSetup.OutlineProperty.AffectsOutline : RenderSetup.OutlineProperty.None)
            .CreateRenderSetup()));

    private static readonly Func<Identifier, bool, RenderType> EntityCutoutZOffsetType = Memoize<Identifier, bool, RenderType>((texture, affectsOutline) => RenderType.Create("entity_cutout_z_offset",
        RenderSetup.Builder(RenderTypePipelines.ENTITY_CUTOUT_Z_OFFSET).WithTexture("Sampler0", texture).UseLightmap().UseOverlay()
            .SetLayeringTransform(LayeringTransform.VIEW_OFFSET_Z_LAYERING).AffectsCrumbling()
            .SetOutline(affectsOutline ? RenderSetup.OutlineProperty.AffectsOutline : RenderSetup.OutlineProperty.None)
            .CreateRenderSetup()));

    private static readonly Func<Identifier, Identifier, RenderType> EntityCutoutDissolveType = Memoize<Identifier, Identifier, RenderType>((texture, maskTexture) => RenderType.Create("entity_cutout_dissolve",
        RenderSetup.Builder(RenderTypePipelines.ENTITY_CUTOUT_DISSOLVE).WithTexture("Sampler0", texture)
            .WithTexture("DissolveMaskSampler", maskTexture).UseLightmap().UseOverlay().AffectsCrumbling()
            .SetOutline(RenderSetup.OutlineProperty.AffectsOutline).CreateRenderSetup()));

    private static readonly Func<Identifier, RenderType> EntityTranslucentCullItemTargetType = Memoize<Identifier, RenderType>(texture => RenderType.Create("entity_translucent_cull_item_target",
        RenderSetup.Builder(RenderTypePipelines.ENTITY_TRANSLUCENT_CULL).WithTexture("Sampler0", texture)
            .SetOutputTarget(OutputTarget.ITEM_ENTITY_TARGET).UseLightmap().UseOverlay().AffectsCrumbling().SortOnUpload()
            .SetOutline(RenderSetup.OutlineProperty.AffectsOutline).CreateRenderSetup()));

    private static readonly Func<Identifier, RenderType> ItemCutoutType = Memoize<Identifier, RenderType>(texture => RenderType.Create("item_cutout",
        RenderSetup.Builder(RenderTypePipelines.ITEM_CUTOUT).WithTexture("Sampler0", texture).UseLightmap().UseOverlay()
            .AffectsCrumbling().SetOutline(RenderSetup.OutlineProperty.AffectsOutline).CreateRenderSetup()));

    private static readonly Func<Identifier, RenderType> ItemTranslucentType = Memoize<Identifier, RenderType>(texture => RenderType.Create("item_translucent",
        RenderSetup.Builder(RenderTypePipelines.ITEM_TRANSLUCENT).WithTexture("Sampler0", texture)
            .SetOutputTarget(OutputTarget.ITEM_ENTITY_TARGET).UseLightmap().UseOverlay().AffectsCrumbling().SortOnUpload()
            .SetOutline(RenderSetup.OutlineProperty.AffectsOutline).CreateRenderSetup()));

    private static readonly Func<Identifier, bool, RenderType> EntityTranslucentType = Memoize<Identifier, bool, RenderType>((texture, affectsOutline) => RenderType.Create("entity_translucent",
        RenderSetup.Builder(RenderTypePipelines.ENTITY_TRANSLUCENT).WithTexture("Sampler0", texture).UseLightmap().UseOverlay()
            .AffectsCrumbling().SortOnUpload()
            .SetOutline(affectsOutline ? RenderSetup.OutlineProperty.AffectsOutline : RenderSetup.OutlineProperty.None)
            .CreateRenderSetup()));

    private static readonly Func<Identifier, bool, RenderType> EntityTranslucentEmissiveType = Memoize<Identifier, bool, RenderType>((texture, affectsOutline) => RenderType.Create("entity_translucent_emissive",
        RenderSetup.Builder(RenderTypePipelines.ENTITY_TRANSLUCENT_EMISSIVE).WithTexture("Sampler0", texture).UseOverlay()
            .AffectsCrumbling().SortOnUpload()
            .SetOutline(affectsOutline ? RenderSetup.OutlineProperty.AffectsOutline : RenderSetup.OutlineProperty.None)
            .CreateRenderSetup()));

    private static readonly Func<Identifier, RenderType> EndCrystalBeamType = Memoize<Identifier, RenderType>(texture => RenderType.Create("end_crystal_beam",
        RenderSetup.Builder(RenderTypePipelines.END_CRYSTAL_BEAM).WithTexture("Sampler0", texture).UseLightmap()
            .SetOutline(RenderSetup.OutlineProperty.None).CreateRenderSetup()));

    private static readonly Func<Identifier, bool, RenderType> BeaconBeamType = Memoize<Identifier, bool, RenderType>((texture, translucent) => RenderType.Create("beacon_beam",
        RenderSetup.Builder(translucent ? RenderTypePipelines.BEACON_BEAM_TRANSLUCENT : RenderTypePipelines.BEACON_BEAM_OPAQUE)
            .WithTexture("Sampler0", texture).SortOnUpload().CreateRenderSetup()));

    private static readonly Func<Identifier, RenderType> BannerPatternType = Memoize<Identifier, RenderType>(texture => RenderType.Create("banner_pattern",
        RenderSetup.Builder(RenderTypePipelines.BANNER_PATTERN).WithTexture("Sampler0", texture).UseLightmap().SortOnUpload().CreateRenderSetup()));

    private static readonly Func<Identifier, RenderType> EntityShadowType = Memoize<Identifier, RenderType>(texture => RenderType.Create("entity_shadow",
        RenderSetup.Builder(RenderTypePipelines.ENTITY_SHADOW).WithTexture("Sampler0", texture).UseLightmap().UseOverlay()
            .SetLayeringTransform(LayeringTransform.VIEW_OFFSET_Z_LAYERING).CreateRenderSetup()));

    private static readonly Func<Identifier, RenderType> EyesType = Memoize<Identifier, RenderType>(texture => RenderType.Create("eyes",
        RenderSetup.Builder(RenderTypePipelines.EYES).WithTexture("Sampler0", texture).SortOnUpload().CreateRenderSetup()));

    private static readonly RenderType LeashType = RenderType.Create("leash",
        RenderSetup.Builder(RenderTypePipelines.LEASH).UseLightmap().CreateRenderSetup());

    private static readonly RenderType WaterMaskType = RenderType.Create("water_mask",
        RenderSetup.Builder(RenderTypePipelines.WATER_MASK).CreateRenderSetup());

    private static readonly RenderType ArmorEntityGlintType = RenderType.Create("armor_entity_glint",
        RenderSetup.Builder(RenderTypePipelines.GLINT).WithTexture("Sampler0", EnchantedGlintArmor)
            .SetTextureTransform(TextureTransform.ARMOR_ENTITY_GLINT_TEXTURING)
            .SetLayeringTransform(LayeringTransform.VIEW_OFFSET_Z_LAYERING).CreateRenderSetup());

    private static readonly RenderType GlintTranslucentType = RenderType.Create("glint_translucent",
        RenderSetup.Builder(RenderTypePipelines.GLINT).WithTexture("Sampler0", EnchantedGlintItem)
            .SetTextureTransform(TextureTransform.GLINT_TEXTURING).SetOutputTarget(OutputTarget.ITEM_ENTITY_TARGET)
            .CreateRenderSetup());

    private static readonly RenderType GlintType = RenderType.Create("glint",
        RenderSetup.Builder(RenderTypePipelines.GLINT).WithTexture("Sampler0", EnchantedGlintItem)
            .SetTextureTransform(TextureTransform.GLINT_TEXTURING).CreateRenderSetup());

    private static readonly RenderType EntityGlintType = RenderType.Create("entity_glint",
        RenderSetup.Builder(RenderTypePipelines.GLINT).WithTexture("Sampler0", EnchantedGlintItem)
            .SetTextureTransform(TextureTransform.ENTITY_GLINT_TEXTURING).CreateRenderSetup());

    private static readonly Func<Identifier, RenderType> CrumblingType = Memoize<Identifier, RenderType>(texture => RenderType.Create("crumbling",
        RenderSetup.Builder(RenderTypePipelines.CRUMBLING).WithTexture("Sampler0", texture).SortOnUpload().CreateRenderSetup()));

    private static readonly Func<Identifier, RenderType> TextType = Memoize<Identifier, RenderType>(texture => RenderType.Create("text",
        RenderSetup.Builder(RenderTypePipelines.TEXT).WithTexture("Sampler0", texture).UseLightmap().CreateRenderSetup()));

    private static readonly RenderType TextBackgroundType = RenderType.Create("text_background",
        RenderSetup.Builder(RenderTypePipelines.TEXT_BACKGROUND).UseLightmap().SortOnUpload().CreateRenderSetup());

    private static readonly Func<Identifier, RenderType> TextGrayscaleType = Memoize<Identifier, RenderType>(texture => RenderType.Create("text_grayscale",
        RenderSetup.Builder(RenderTypePipelines.TEXT_GRAYSCALE).WithTexture("Sampler0", texture).UseLightmap().CreateRenderSetup()));

    private static readonly Func<Identifier, RenderType> TextPolygonOffsetType = Memoize<Identifier, RenderType>(texture => RenderType.Create("text_polygon_offset",
        RenderSetup.Builder(RenderTypePipelines.TEXT_POLYGON_OFFSET).WithTexture("Sampler0", texture).UseLightmap().SortOnUpload().CreateRenderSetup()));

    private static readonly Func<Identifier, RenderType> TextGrayscalePolygonOffsetType = Memoize<Identifier, RenderType>(texture => RenderType.Create("text_grayscale_polygon_offset",
        RenderSetup.Builder(RenderTypePipelines.TEXT_GRAYSCALE_POLYGON_OFFSET).WithTexture("Sampler0", texture).UseLightmap().SortOnUpload().CreateRenderSetup()));

    private static readonly Func<Identifier, RenderType> TextSeeThroughType = Memoize<Identifier, RenderType>(texture => RenderType.Create("text_see_through",
        RenderSetup.Builder(RenderTypePipelines.TEXT_SEE_THROUGH).WithTexture("Sampler0", texture).UseLightmap().CreateRenderSetup()));

    private static readonly RenderType TextBackgroundSeeThroughType = RenderType.Create("text_background_see_through",
        RenderSetup.Builder(RenderTypePipelines.TEXT_BACKGROUND_SEE_THROUGH).UseLightmap().SortOnUpload().CreateRenderSetup());

    private static readonly Func<Identifier, RenderType> TextGrayscaleSeeThroughType = Memoize<Identifier, RenderType>(texture => RenderType.Create("text_grayscale_see_through",
        RenderSetup.Builder(RenderTypePipelines.TEXT_GRAYSCALE_SEE_THROUGH).WithTexture("Sampler0", texture).UseLightmap().SortOnUpload().CreateRenderSetup()));

    private static readonly RenderType LightningType = RenderType.Create("lightning",
        RenderSetup.Builder(RenderTypePipelines.LIGHTNING).SetOutputTarget(OutputTarget.WEATHER_TARGET).SortOnUpload().CreateRenderSetup());

    private static readonly RenderType DragonRaysType = RenderType.Create("dragon_rays",
        RenderSetup.Builder(RenderTypePipelines.DRAGON_RAYS).CreateRenderSetup());

    private static readonly RenderType EndPortalType = RenderType.Create("end_portal",
        RenderSetup.Builder(RenderTypePipelines.END_PORTAL).WithTexture("Sampler0", EndSkyLocation)
            .WithTexture("Sampler1", EndPortalLocation).CreateRenderSetup());

    private static readonly RenderType EndGatewayType = RenderType.Create("end_gateway",
        RenderSetup.Builder(RenderTypePipelines.END_GATEWAY).WithTexture("Sampler0", EndSkyLocation)
            .WithTexture("Sampler1", EndPortalLocation).CreateRenderSetup());

    public static readonly RenderType LINES = RenderType.Create("lines",
        RenderSetup.Builder(RenderTypePipelines.LINES).SetLayeringTransform(LayeringTransform.VIEW_OFFSET_Z_LAYERING)
            .SetOutputTarget(OutputTarget.ITEM_ENTITY_TARGET).CreateRenderSetup());

    public static readonly RenderType LINES_TRANSLUCENT = RenderType.Create("lines_translucent",
        RenderSetup.Builder(RenderTypePipelines.LINES_TRANSLUCENT).SetLayeringTransform(LayeringTransform.VIEW_OFFSET_Z_LAYERING)
            .SetOutputTarget(OutputTarget.ITEM_ENTITY_TARGET).CreateRenderSetup());

    public static readonly RenderType SECONDARY_BLOCK_OUTLINE = RenderType.Create("secondary_block_outline",
        RenderSetup.Builder(RenderTypePipelines.SECONDARY_BLOCK_OUTLINE).SetLayeringTransform(LayeringTransform.VIEW_OFFSET_Z_LAYERING)
            .SetOutputTarget(OutputTarget.ITEM_ENTITY_TARGET).CreateRenderSetup());

    private static readonly RenderType DebugFilledBoxType = RenderType.Create("debug_filled_box",
        RenderSetup.Builder(RenderTypePipelines.DEBUG_FILLED_BOX).SortOnUpload()
            .SetLayeringTransform(LayeringTransform.VIEW_OFFSET_Z_LAYERING).CreateRenderSetup());

    private static readonly RenderType DebugPointType = RenderType.Create("debug_point",
        RenderSetup.Builder(RenderTypePipelines.DEBUG_POINTS).CreateRenderSetup());

    private static readonly RenderType DebugQuadsType = RenderType.Create("debug_quads",
        RenderSetup.Builder(RenderTypePipelines.DEBUG_QUADS).SortOnUpload().CreateRenderSetup());

    private static readonly RenderType DebugTriangleFanType = RenderType.Create("debug_triangle_fan",
        RenderSetup.Builder(RenderTypePipelines.DEBUG_TRIANGLE_FAN).CreateRenderSetup());

    private static readonly Func<Identifier, RenderType> BlockScreenEffectType = Memoize<Identifier, RenderType>(texture => RenderType.Create("block_screen_effect",
        RenderSetup.Builder(RenderTypePipelines.BLOCK_SCREEN_EFFECT).WithTexture("Sampler0", texture).CreateRenderSetup()));

    private static readonly Func<Identifier, RenderType> FireScreenEffectType = Memoize<Identifier, RenderType>(texture => RenderType.Create("fire_screen_effect",
        RenderSetup.Builder(RenderTypePipelines.FIRE_SCREEN_EFFECT).WithTexture("Sampler0", texture).CreateRenderSetup()));

    static RenderTypes()
    {
        RenderType.SetOutlineFactory((texture, cull) => OutlineFactory(texture, cull));
    }

    private static RenderSetup CreateMovingBlockSetup(RenderPipeline pipeline, bool translucent)
    {
        var setup = RenderSetup.Builder(pipeline).UseLightmap()
            .WithTexture("Sampler0", TextureAtlasLocation,
                () => RenderSystem.GetSamplerCache().GetSampler(AddressMode.ClampToEdge, AddressMode.ClampToEdge,
                    FilterMode.Linear, FilterMode.Nearest, true))
            .AffectsCrumbling().SetOutline(RenderSetup.OutlineProperty.AffectsOutline);
        if (translucent)
        {
            setup.SortOnUpload();
            setup.SetOutputTarget(OutputTarget.ITEM_ENTITY_TARGET);
        }

        return setup.CreateRenderSetup();
    }

    public static RenderType SolidMovingBlock() => SOLID_MOVING_BLOCK;

    public static RenderType CutoutMovingBlock() => CUTOUT_MOVING_BLOCK;

    public static RenderType TranslucentMovingBlock() => TRANSLUCENT_MOVING_BLOCK;

    public static RenderType ArmorCutoutNoCull(Identifier texture) => ArmorCutoutNoCullType(texture);

    public static RenderType CreateArmorDecalCutoutNoCull(Identifier texture)
        => RenderType.Create("armor_decal_cutout_no_cull",
            RenderSetup.Builder(RenderTypePipelines.ARMOR_DECAL_CUTOUT_NO_CULL).WithTexture("Sampler0", texture)
                .UseLightmap().UseOverlay().SetLayeringTransform(LayeringTransform.VIEW_OFFSET_Z_LAYERING).AffectsCrumbling()
                .SetOutline(RenderSetup.OutlineProperty.AffectsOutline).CreateRenderSetup());

    public static RenderType ArmorTranslucent(Identifier texture) => ArmorTranslucentType(texture);

    public static RenderType EntitySolid(Identifier texture) => EntitySolidType(texture);

    public static RenderType EntitySolidZOffsetForward(Identifier texture) => EntitySolidZOffsetForwardType(texture);

    public static RenderType EntityCutoutCull(Identifier texture) => EntityCutoutCullType(texture);

    public static RenderType EntityCutout(Identifier texture, bool affectsOutline) => EntityCutoutType(texture, affectsOutline);

    public static RenderType EntityCutout(Identifier texture) => EntityCutoutType(texture, true);

    public static RenderType EntityCutoutZOffset(Identifier texture, bool affectsOutline) => EntityCutoutZOffsetType(texture, affectsOutline);

    public static RenderType EntityCutoutZOffset(Identifier texture) => EntityCutoutZOffsetType(texture, true);

    public static RenderType EntityCutoutDissolve(Identifier texture, Identifier maskTexture) => EntityCutoutDissolveType(texture, maskTexture);

    public static RenderType EntityTranslucentCullItemTarget(Identifier texture) => EntityTranslucentCullItemTargetType(texture);

    public static RenderType ItemCutout(Identifier texture) => ItemCutoutType(texture);

    public static RenderType ItemTranslucent(Identifier texture) => ItemTranslucentType(texture);

    public static RenderType EntityTranslucent(Identifier texture, bool affectsOutline) => EntityTranslucentType(texture, affectsOutline);

    public static RenderType EntityTranslucent(Identifier texture) => EntityTranslucentType(texture, true);

    public static RenderType EntityTranslucentEmissive(Identifier texture, bool affectsOutline) => EntityTranslucentEmissiveType(texture, affectsOutline);

    public static RenderType EntityTranslucentEmissive(Identifier texture) => EntityTranslucentEmissiveType(texture, true);

    public static RenderType EndCrystalBeam(Identifier texture) => EndCrystalBeamType(texture);

    public static RenderType BeaconBeam(Identifier texture, bool translucent) => BeaconBeamType(texture, translucent);

    public static RenderType BannerPattern(Identifier texture) => BannerPatternType(texture);

    public static RenderType EntityShadow(Identifier texture) => EntityShadowType(texture);

    public static RenderType Eyes(Identifier texture) => EyesType(texture);

    public static RenderType BreezeEyes(Identifier texture) => EntityTranslucentEmissiveType(texture, false);

    public static RenderType BreezeWind(Identifier texture, float uOffset, float vOffset)
        => RenderType.Create("breeze_wind", RenderSetup.Builder(RenderTypePipelines.BREEZE_WIND).WithTexture("Sampler0", texture)
            .SetTextureTransform(new TextureTransform.OffsetTextureTransform(uOffset, vOffset)).UseLightmap().SortOnUpload()
            .CreateRenderSetup());

    public static RenderType EnergySwirl(Identifier texture, float uOffset, float vOffset)
        => RenderType.Create("energy_swirl", RenderSetup.Builder(RenderTypePipelines.ENERGY_SWIRL).WithTexture("Sampler0", texture)
            .SetTextureTransform(new TextureTransform.OffsetTextureTransform(uOffset, vOffset)).UseLightmap().UseOverlay()
            .SortOnUpload().CreateRenderSetup());

    public static RenderType Leash() => LeashType;

    public static RenderType WaterMask() => WaterMaskType;

    public static RenderType Outline(Identifier texture) => OutlineFactory(texture, false);

    public static RenderType ArmorEntityGlint() => ArmorEntityGlintType;

    public static RenderType GlintTranslucent() => GlintTranslucentType;

    public static RenderType Glint() => GlintType;

    public static RenderType EntityGlint() => EntityGlintType;

    public static RenderType Crumbling(Identifier texture) => CrumblingType(texture);

    public static RenderType Text(Identifier texture) => TextType(texture);

    public static RenderType TextBackground() => TextBackgroundType;

    public static RenderType TextGrayscale(Identifier texture) => TextGrayscaleType(texture);

    public static RenderType TextPolygonOffset(Identifier texture) => TextPolygonOffsetType(texture);

    public static RenderType TextGrayscalePolygonOffset(Identifier texture) => TextGrayscalePolygonOffsetType(texture);

    public static RenderType TextSeeThrough(Identifier texture) => TextSeeThroughType(texture);

    public static RenderType TextBackgroundSeeThrough() => TextBackgroundSeeThroughType;

    public static RenderType TextGrayscaleSeeThrough(Identifier texture) => TextGrayscaleSeeThroughType(texture);

    public static RenderType Lightning() => LightningType;

    public static RenderType DragonRays() => DragonRaysType;

    public static RenderType EndPortal() => EndPortalType;

    public static RenderType EndGateway() => EndGatewayType;

    public static RenderType Lines() => LINES;

    public static RenderType LinesTranslucent() => LINES_TRANSLUCENT;

    public static RenderType SecondaryBlockOutline() => SECONDARY_BLOCK_OUTLINE;

    public static RenderType DebugFilledBox() => DebugFilledBoxType;

    public static RenderType DebugPoint() => DebugPointType;

    public static RenderType DebugQuads() => DebugQuadsType;

    public static RenderType DebugTriangleFan() => DebugTriangleFanType;

    public static RenderType BlockScreenEffect(Identifier texture) => BlockScreenEffectType(texture);

    public static RenderType FireScreenEffect(Identifier texture) => FireScreenEffectType(texture);

    //Memoize caches one result per input, maps to vanilla Util.memoize
    private static Func<T, R> Memoize<T, R>(Func<T, R> fn) where T : notnull
    {
        var cache = new ConcurrentDictionary<T, R>();
        return input => cache.GetOrAdd(input, fn);
    }

    //Memoize caches one result per input pair
    private static Func<T1, T2, R> Memoize<T1, T2, R>(Func<T1, T2, R> fn) where T1 : notnull where T2 : notnull
    {
        var cache = new ConcurrentDictionary<(T1, T2), R>();
        return (a, b) => cache.GetOrAdd((a, b), key => fn(key.Item1, key.Item2));
    }
}
