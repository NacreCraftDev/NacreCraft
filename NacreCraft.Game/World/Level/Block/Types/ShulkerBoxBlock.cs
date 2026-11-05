using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//ShulkerBoxBlock block type, maps to vanilla net.minecraft.world.level.block.ShulkerBoxBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class ShulkerBoxBlock : BlockBehaviour
{
    private readonly string _name;

    public ShulkerBoxBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
