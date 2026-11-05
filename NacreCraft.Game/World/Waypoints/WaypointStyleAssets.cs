using NacreCraft.Registry;

namespace NacreCraft.Game.World.Waypoints;

//WaypointStyleAssets built-in waypoint style keys, maps to vanilla net.minecraft.world.waypoints.WaypointStyleAssets
public static class WaypointStyleAssets
{
    //ROOT_ID the waypoint style asset registry key
    public static readonly ResourceKey<Registry<WaypointStyleAsset>> ROOT_ID =
        ResourceKeys.CreateRegistryKey<WaypointStyleAsset>(Identifier.WithDefaultNamespace("waypoint_style_asset"));

    public static readonly ResourceKey<WaypointStyleAsset> DEFAULT = CreateId("default");
    public static readonly ResourceKey<WaypointStyleAsset> BOWTIE = CreateId("bowtie");

    //CreateId builds a style key inside the waypoint style asset registry
    public static ResourceKey<WaypointStyleAsset> CreateId(string name)
        => ResourceKey<WaypointStyleAsset>.Create(ROOT_ID, Identifier.WithDefaultNamespace(name));
}
