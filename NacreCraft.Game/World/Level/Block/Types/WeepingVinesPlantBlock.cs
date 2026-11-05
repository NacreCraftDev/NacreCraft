using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//WeepingVinesPlantBlock block type, maps to vanilla net.minecraft.world.level.block.WeepingVinesPlantBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class WeepingVinesPlantBlock : BlockBehaviour
{
    private readonly string _name;

    public WeepingVinesPlantBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
