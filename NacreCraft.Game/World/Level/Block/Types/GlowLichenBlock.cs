using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//GlowLichenBlock block type, maps to vanilla net.minecraft.world.level.block.GlowLichenBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class GlowLichenBlock : BlockBehaviour
{
    private readonly string _name;

    public GlowLichenBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
