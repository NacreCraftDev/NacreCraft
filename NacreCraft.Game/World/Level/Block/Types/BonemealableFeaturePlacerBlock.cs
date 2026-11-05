using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//BonemealableFeaturePlacerBlock block type, maps to vanilla net.minecraft.world.level.block.BonemealableFeaturePlacerBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class BonemealableFeaturePlacerBlock : BlockBehaviour
{
    private readonly string _name;

    public BonemealableFeaturePlacerBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
