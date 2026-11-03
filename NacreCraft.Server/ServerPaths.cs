namespace NacreCraft.Server;

//ServerPaths holds the paths that describe the server layout
//
//The world root belongs here rather than in AppPaths because it is not shared: the server keeps each world directly
//under the program root, one folder per level-name, while a client keeps every world in its own subdirectory under a
//client-specific root. AppPaths stays the common base both derive from; what only one end understands is defined here
public static class ServerPaths
{
    //WorldsRoot the directory the world folders sit in, which is the program root itself
    //Vanilla names the overworld <root>/<level-name>, the same level the logs, server.properties and the jar live in,
    //so there is no worlds/ layer in between
    public static string WorldsRoot => AppPaths.BaseDirectory;
}
