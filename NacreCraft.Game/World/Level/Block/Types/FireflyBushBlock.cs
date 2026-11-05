using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//FireflyBushBlock block type, maps to vanilla net.minecraft.world.level.block.FireflyBushBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class FireflyBushBlock : BlockBehaviour
{
    private readonly string _name;

    public FireflyBushBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
