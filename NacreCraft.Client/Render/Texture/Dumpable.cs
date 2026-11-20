using NacreCraft.Registry;

namespace NacreCraft.Client.Render.Texture;

//Dumpable a texture that can write its pixels to disk for debugging, maps to vanilla net.minecraft.client.renderer.texture.Dumpable
public interface Dumpable
{
    void DumpContents(Identifier selfId, string directory);
}
