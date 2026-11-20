namespace NacreCraft.Client.Render.Texture;

//TickableTexture a texture that advances animation state once per client tick, maps to vanilla net.minecraft.client.renderer.texture.TickableTexture
public interface TickableTexture
{
    void Tick();
}
