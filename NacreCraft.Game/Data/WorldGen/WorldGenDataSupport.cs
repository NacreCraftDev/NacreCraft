using NacreCraft.Game.World.Level.Block;
using NacreCraft.Registry;
using NacreCraft.Registry.State;

namespace NacreCraft.Game.Data.WorldGen;

//WorldGenDataSupport registry-name lookups for the worldgen data tables, maps to the vanilla Blocks constants those tables use
//This project registers blocks in stages; a table that names a block which is not registered yet falls back to air so the remaining
//entries still register, and the lookup starts returning the real block as soon as it is filled in
internal static class WorldGenDataSupport
{
    //Block fetch a block by registry path, falling back to air
    public static NacreCraft.Registry.Block Block(string path)
        => BuiltInRegistries.BLOCK.GetValue(Identifier.WithDefaultNamespace(path)) ?? Blocks.AIR;

    //State default state of a block by registry path, falling back to air
    public static BlockState State(string path) => Block(path).DefaultBlockState;
}
