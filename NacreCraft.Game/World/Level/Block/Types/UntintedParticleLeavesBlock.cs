using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//UntintedParticleLeavesBlock block type, maps to vanilla net.minecraft.world.level.block.UntintedParticleLeavesBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class UntintedParticleLeavesBlock : BlockBehaviour
{
    private readonly string _name;

    public UntintedParticleLeavesBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
