using NacreCraft.Resources;
using NacreCraft.Registry;

namespace NacreCraft.Client.Render.Texture;

//SimpleTexture a texture loaded from one PNG in the resource packs, maps to vanilla net.minecraft.client.renderer.texture.SimpleTexture
public sealed class SimpleTexture : ReloadableTexture
{
    private readonly ResourceManager _resourceManager;

    public SimpleTexture(Identifier location, ResourceManager resourceManager) : base(location)
    {
        _resourceManager = resourceManager;
        Apply(LoadContents(resourceManager));
    }

    public override TextureContents LoadContents(ResourceManager resourceManager)
        => TextureContents.Load(resourceManager, ResourceId);

    //Reload reapplies the current resource contents, used by TextureManager.Reload
    public void Reload() => Apply(LoadContents(_resourceManager));
}
