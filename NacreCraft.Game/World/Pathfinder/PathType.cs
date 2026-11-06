namespace NacreCraft.Game.World.Pathfinder;

//PathType the movement class of one cell, maps to vanilla net.minecraft.world.level.pathfinder.PathType
//The value is the additional cost (malus) the cell adds to the path
public enum PathType
{
    BLOCKED,
    OPEN,
    WALKABLE,
    WALKABLE_DOOR,
    TRAPDOOR,
    POWDER_SNOW,
    ON_TOP_OF_POWDER_SNOW,
    FENCE,
    LAVA,
    WATER,
    WATER_BORDER,
    RAIL,
    UNPASSABLE_RAIL,
    FIRE_IN_NEIGHBOR,
    FIRE,
    DAMAGING_IN_NEIGHBOR,
    DAMAGING,
    DOOR_OPEN,
    DOOR_WOOD_CLOSED,
    DOOR_IRON_CLOSED,
    BREACH,
    LEAVES,
    STICKY_HONEY,
    COCOA,
    DAMAGE_CAUTIOUS,
    ON_TOP_OF_TRAPDOOR,
    BIG_MOBS_CLOSE_TO_DANGER
}

public static class PathTypes
{
    //Malus the extra path cost of the type, maps to vanilla getMalus
    public static float GetMalus(this PathType type) => type switch
    {
        PathType.BLOCKED or PathType.POWDER_SNOW or PathType.FENCE or PathType.LAVA
            or PathType.UNPASSABLE_RAIL or PathType.DAMAGING or PathType.DOOR_WOOD_CLOSED
            or PathType.DOOR_IRON_CLOSED or PathType.LEAVES => -1.0f,
        PathType.WATER or PathType.WATER_BORDER or PathType.FIRE_IN_NEIGHBOR
            or PathType.DAMAGING_IN_NEIGHBOR or PathType.STICKY_HONEY => 8.0f,
        PathType.FIRE => 16.0f,
        PathType.BREACH or PathType.BIG_MOBS_CLOSE_TO_DANGER => 4.0f,
        _ => 0.0f
    };
}
