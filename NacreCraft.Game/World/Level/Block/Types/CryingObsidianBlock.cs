using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//CryingObsidianBlock block type, maps to vanilla net.minecraft.world.level.block.CryingObsidianBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class CryingObsidianBlock : BlockBehaviour
{
    private readonly string _name;

    public CryingObsidianBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
