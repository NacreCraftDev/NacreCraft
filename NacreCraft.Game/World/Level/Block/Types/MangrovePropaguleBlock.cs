using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//MangrovePropaguleBlock block type, maps to vanilla net.minecraft.world.level.block.MangrovePropaguleBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class MangrovePropaguleBlock : BlockBehaviour
{
    private readonly string _name;

    public MangrovePropaguleBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
