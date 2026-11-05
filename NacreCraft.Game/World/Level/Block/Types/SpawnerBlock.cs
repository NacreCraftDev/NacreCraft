using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//SpawnerBlock block type, maps to vanilla net.minecraft.world.level.block.SpawnerBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class SpawnerBlock : BlockBehaviour
{
    private readonly string _name;

    public SpawnerBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
