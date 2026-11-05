using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//SignBlock block type, maps to vanilla net.minecraft.world.level.block.SignBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class SignBlock : BlockBehaviour
{
    private readonly string _name;

    public SignBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
