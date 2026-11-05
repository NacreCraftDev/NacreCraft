using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//JukeboxBlock block type, maps to vanilla net.minecraft.world.level.block.JukeboxBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class JukeboxBlock : BlockBehaviour
{
    private readonly string _name;

    public JukeboxBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
