using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//CaveVinesPlantBlock block type, maps to vanilla net.minecraft.world.level.block.CaveVinesPlantBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class CaveVinesPlantBlock : BlockBehaviour
{
    private readonly string _name;

    public CaveVinesPlantBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
