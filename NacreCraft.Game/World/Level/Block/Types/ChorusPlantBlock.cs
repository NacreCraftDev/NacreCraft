using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//ChorusPlantBlock block type, maps to vanilla net.minecraft.world.level.block.ChorusPlantBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class ChorusPlantBlock : BlockBehaviour
{
    private readonly string _name;

    public ChorusPlantBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
