using NacreCraft.Client.Render.Rendertype;
using NacreCraft.Registry;

namespace NacreCraft.Client.Render.Model.Sprite;

//SpriteId identifies a sprite by its atlas and texture, maps to vanilla net.minecraft.client.resources.model.sprite.SpriteId
public readonly record struct SpriteId(Identifier AtlasLocation, Identifier Texture)
{
    public RenderType RenderType(Func<Identifier, RenderType> renderType) => renderType(AtlasLocation);
}
