namespace NacreCraft.Client.Render.Texture;

//StitcherException thrown when a sprite cannot fit in the atlas, maps to vanilla net.minecraft.client.renderer.texture.StitcherException
public sealed class StitcherException : Exception
{
    public IReadOnlyCollection<StitcherEntry> AllSprites { get; }

    public StitcherException(StitcherEntry sprite, IReadOnlyCollection<StitcherEntry> allSprites)
        : base($"Unable to fit: {sprite.Name} - size: {sprite.Width}x{sprite.Height} - Maybe try a lower resolution resourcepack?")
    {
        AllSprites = allSprites;
    }
}
