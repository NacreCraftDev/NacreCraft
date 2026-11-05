namespace NacreCraft.Game.World.Level.LevelGen.Dimension;

//ChunkGenerators built-in chunk generator registration, maps to vanilla net.minecraft.world.level.chunk.ChunkGenerators
//The registration lives in ChunkGeneratorTypes; this entry keeps the vanilla bootstrap name
public static class ChunkGenerators
{
    public static void Bootstrap() => ChunkGeneratorTypes.RegisterAll();
}
