using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//MangroveRootsBlock block type, maps to vanilla net.minecraft.world.level.block.MangroveRootsBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class MangroveRootsBlock : BlockBehaviour
{
    private readonly string _name;

    public MangroveRootsBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
