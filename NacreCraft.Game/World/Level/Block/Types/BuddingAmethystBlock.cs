using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//BuddingAmethystBlock block type, maps to vanilla net.minecraft.world.level.block.BuddingAmethystBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class BuddingAmethystBlock : BlockBehaviour
{
    private readonly string _name;

    public BuddingAmethystBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
