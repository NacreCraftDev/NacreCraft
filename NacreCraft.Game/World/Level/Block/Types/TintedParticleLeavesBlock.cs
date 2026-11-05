using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//TintedParticleLeavesBlock block type, maps to vanilla net.minecraft.world.level.block.TintedParticleLeavesBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class TintedParticleLeavesBlock : BlockBehaviour
{
    private readonly string _name;

    public TintedParticleLeavesBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
