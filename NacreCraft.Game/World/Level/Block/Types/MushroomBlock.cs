using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//MushroomBlock block type, maps to vanilla net.minecraft.world.level.block.MushroomBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class MushroomBlock : BlockBehaviour
{
    private readonly string _name;

    public MushroomBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
