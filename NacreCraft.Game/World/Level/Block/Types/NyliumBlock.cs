using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//NyliumBlock block type, maps to vanilla net.minecraft.world.level.block.NyliumBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class NyliumBlock : BlockBehaviour
{
    private readonly string _name;

    public NyliumBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
