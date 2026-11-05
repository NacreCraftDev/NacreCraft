using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//BambooSaplingBlock block type, maps to vanilla net.minecraft.world.level.block.BambooSaplingBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class BambooSaplingBlock : BlockBehaviour
{
    private readonly string _name;

    public BambooSaplingBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
