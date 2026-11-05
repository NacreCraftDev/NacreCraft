using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//StandingSignBlock block type, maps to vanilla net.minecraft.world.level.block.StandingSignBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class StandingSignBlock : BlockBehaviour
{
    private readonly string _name;

    public StandingSignBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
