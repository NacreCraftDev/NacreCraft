using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//CocoaBlock block type, maps to vanilla net.minecraft.world.level.block.CocoaBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class CocoaBlock : BlockBehaviour
{
    private readonly string _name;

    public CocoaBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
