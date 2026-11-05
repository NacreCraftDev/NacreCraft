using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//SlimeBlock block type, maps to vanilla net.minecraft.world.level.block.SlimeBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class SlimeBlock : BlockBehaviour
{
    private readonly string _name;

    public SlimeBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
