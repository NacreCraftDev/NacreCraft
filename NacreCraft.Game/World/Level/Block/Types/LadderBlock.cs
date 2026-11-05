using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//LadderBlock block type, maps to vanilla net.minecraft.world.level.block.LadderBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class LadderBlock : BlockBehaviour
{
    private readonly string _name;

    public LadderBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
