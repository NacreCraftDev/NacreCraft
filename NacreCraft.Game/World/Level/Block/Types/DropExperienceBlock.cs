using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//DropExperienceBlock block type, maps to vanilla net.minecraft.world.level.block.DropExperienceBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class DropExperienceBlock : BlockBehaviour
{
    private readonly string _name;

    public DropExperienceBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
