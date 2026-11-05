using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//StainedGlassBlock block type, maps to vanilla net.minecraft.world.level.block.StainedGlassBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class StainedGlassBlock : BlockBehaviour
{
    private readonly string _name;

    public StainedGlassBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
