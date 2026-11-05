using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//IceBlock block type, maps to vanilla net.minecraft.world.level.block.IceBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class IceBlock : BlockBehaviour
{
    private readonly string _name;

    public IceBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
