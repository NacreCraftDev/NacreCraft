using NacreCraft.Client.Render.Texture;
using NacreCraft.Network.Chat;
using NacreCraft.Registry;

namespace NacreCraft.Client.Render.State;

//MapRenderState one map's texture and decoration snapshot, maps to vanilla net.minecraft.client.renderer.state.MapRenderState
public sealed class MapRenderState
{
    public Identifier? Texture;
    public readonly List<MapDecorationRenderState> Decorations = new();

    //MapDecorationRenderState one decoration on the map, maps to vanilla MapRenderState.MapDecorationRenderState
    public sealed class MapDecorationRenderState
    {
        public TextureAtlasSprite? AtlasSprite;
        public sbyte X;
        public sbyte Y;
        public sbyte Rot;
        public bool RenderOnFrame;
        public Component? Name;
    }
}
