using NacreCraft.Client.Render.Texture;
using NacreCraft.Registry;

namespace NacreCraft.Client.Render.Model.Sprite;

//Material an un-baked sprite reference, maps to vanilla net.minecraft.client.resources.model.sprite.Material
public sealed record Material(Identifier Sprite, bool ForceTranslucent = false)
{
    public Material WithForceTranslucent(bool forceTranslucent) => this with { ForceTranslucent = forceTranslucent };

    //Baked a material resolved to an actual atlas sprite
    public sealed record Baked(TextureAtlasSprite Sprite, bool ForceTranslucent);
}
