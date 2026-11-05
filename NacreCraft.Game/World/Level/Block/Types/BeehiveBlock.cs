using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//BeehiveBlock block type, maps to vanilla net.minecraft.world.level.block.BeehiveBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class BeehiveBlock : BlockBehaviour
{
    private readonly string _name;

    public BeehiveBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
