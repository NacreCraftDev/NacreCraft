using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//StainedGlassPaneBlock block type, maps to vanilla net.minecraft.world.level.block.StainedGlassPaneBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class StainedGlassPaneBlock : BlockBehaviour
{
    private readonly string _name;

    public StainedGlassPaneBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
