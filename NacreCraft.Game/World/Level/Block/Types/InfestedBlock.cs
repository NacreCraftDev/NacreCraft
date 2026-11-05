using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//InfestedBlock block type, maps to vanilla net.minecraft.world.level.block.InfestedBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class InfestedBlock : BlockBehaviour
{
    private readonly string _name;

    public InfestedBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
