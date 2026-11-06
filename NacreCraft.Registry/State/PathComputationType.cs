namespace NacreCraft.Registry.State;

//PathComputationType which movement mode a path query uses, maps to vanilla net.minecraft.world.level.pathfinder.PathComputationType
//Lives in the registry layer because BlockState.isPathfindable forwards it into block behavior
public enum PathComputationType
{
    LAND,
    WATER,
    AIR
}
