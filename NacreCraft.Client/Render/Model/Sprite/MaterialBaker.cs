using NacreCraft.Client.Render.Texture;
using NacreCraft.Registry;

namespace NacreCraft.Client.Render.Model.Sprite;

//MaterialBaker resolves Material references to atlas sprites and caches the result, maps to vanilla
//net.minecraft.client.resources.model.sprite.MaterialBaker
//NC's subclass supplies the sprite lookup through SpriteGetter, the vanilla AtlasManager path is not ported yet
public abstract class MaterialBaker
{
    private readonly Material.Baked _missingSprite;
    private readonly Material.Baked _missingSpriteForceTranslucent;
    private readonly Dictionary<Material, Material.Baked?> _bakedMaterials = new();
    private readonly HashSet<string> _missingReferences = new();

    protected MaterialBaker(TextureAtlasSprite missingSprite)
    {
        _missingSprite = new Material.Baked(missingSprite, false);
        _missingSpriteForceTranslucent = new Material.Baked(missingSprite, true);
    }

    public Material.Baked ReplacementForMissingMaterial(Material material)
        => material.ForceTranslucent ? _missingSpriteForceTranslucent : _missingSprite;

    public Material.Baked Get(Material material, ModelDebugName name)
    {
        if (_bakedMaterials.TryGetValue(material, out var cached))
            return cached ?? ReplacementForMissingMaterial(material);

        var baked = Bake(material);
        _bakedMaterials[material] = baked;
        return baked ?? ReplacementForMissingMaterial(material);
    }

    public Material.Baked ResolveSlot(TextureSlots slots, string id, ModelDebugName name)
    {
        var material = slots.GetMaterial(id);
        if (material is not null) return Get(material, name);
        _missingReferences.Add($"{name.DebugName()}:{id}");
        return _missingSprite;
    }

    public void LogMissingTextures()
    {
    }

    protected abstract Material.Baked? Bake(Material material);

    //BakeForSprite builds a baked material when the atlas has the sprite
    protected static Material.Baked? BakeForSprite(Material material, TextureAtlasSprite? sprite)
        => sprite is null ? null : new Material.Baked(sprite, material.ForceTranslucent);
}
