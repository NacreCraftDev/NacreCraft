using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//TrialSpawnerBlock block type, maps to vanilla net.minecraft.world.level.block.TrialSpawnerBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class TrialSpawnerBlock : BlockBehaviour
{
    private readonly string _name;

    public TrialSpawnerBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
