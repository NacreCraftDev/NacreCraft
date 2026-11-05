using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//FlowerPotBlock block type, maps to vanilla net.minecraft.world.level.block.FlowerPotBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class FlowerPotBlock : BlockBehaviour
{
    private readonly string _name;

    public FlowerPotBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
