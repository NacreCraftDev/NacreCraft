using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//MudBlock block type, maps to vanilla net.minecraft.world.level.block.MudBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class MudBlock : BlockBehaviour
{
    private readonly string _name;

    public MudBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
