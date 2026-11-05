using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//HalfTransparentBlock block type, maps to vanilla net.minecraft.world.level.block.HalfTransparentBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class HalfTransparentBlock : BlockBehaviour
{
    private readonly string _name;

    public HalfTransparentBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
