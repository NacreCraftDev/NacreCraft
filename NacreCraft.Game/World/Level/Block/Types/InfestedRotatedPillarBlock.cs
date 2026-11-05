using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//InfestedRotatedPillarBlock block type, maps to vanilla net.minecraft.world.level.block.InfestedRotatedPillarBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class InfestedRotatedPillarBlock : BlockBehaviour
{
    private readonly string _name;

    public InfestedRotatedPillarBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
