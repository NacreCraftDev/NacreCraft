using NacreCraft.Client.Render.Texture;
using NacreCraft.Client.Render.Model.Sprite;

namespace NacreCraft.Client.Render.Model.Sprite;

//SpriteGetter resolves a SpriteId to its atlas sprite, maps to vanilla net.minecraft.client.resources.model.sprite.SpriteGetter
public interface SpriteGetter
{
    TextureAtlasSprite? Get(SpriteId id);
}
