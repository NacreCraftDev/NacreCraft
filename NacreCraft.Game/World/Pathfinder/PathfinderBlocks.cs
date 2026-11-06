using NacreCraft.Registry;
using NacreCraft.Registry.State;

namespace NacreCraft.Game.World.Pathfinder;

//PathfinderBlocks block lookups needed by path type rules, maps to vanilla Blocks references
//Blocks that are not registered in this project yet (powder snow, big dripleaf, cocoa, wither rose and the burning blocks)
//are matched by registry name instead, so the rules keep working as those blocks get filled in
internal static class PathfinderBlocks
{
    //IsNamed whether the state's block has the given vanilla registry path
    public static bool IsNamed(BlockState state, string path)
    {
        var id = state.Owner.Id;
        return id.Namespace == Identifier.DefaultNamespace && id.Path == path;
    }
}
