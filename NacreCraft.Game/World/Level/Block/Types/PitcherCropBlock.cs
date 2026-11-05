using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//PitcherCropBlock block type, maps to vanilla net.minecraft.world.level.block.PitcherCropBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class PitcherCropBlock : BlockBehaviour
{
    private readonly string _name;

    public PitcherCropBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
