using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//KelpPlantBlock block type, maps to vanilla net.minecraft.world.level.block.KelpPlantBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class KelpPlantBlock : BlockBehaviour
{
    private readonly string _name;

    public KelpPlantBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
