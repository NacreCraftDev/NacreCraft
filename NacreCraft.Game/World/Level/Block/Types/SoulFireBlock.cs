using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//SoulFireBlock block type, maps to vanilla net.minecraft.world.level.block.SoulFireBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class SoulFireBlock : BlockBehaviour
{
    private readonly string _name;

    public SoulFireBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
