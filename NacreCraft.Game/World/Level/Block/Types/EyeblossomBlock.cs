using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//EyeblossomBlock block type, maps to vanilla net.minecraft.world.level.block.EyeblossomBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class EyeblossomBlock : BlockBehaviour
{
    private readonly string _name;

    public EyeblossomBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
