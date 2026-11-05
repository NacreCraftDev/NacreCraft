using NacreCraft.Storage.Chunk;

namespace NacreCraft.Storage.Light;

//ThreadedLevelLightEngine light engine wrapper, maps to vanilla net.minecraft.server.level.ThreadedLevelLightEngine
//Vanilla hands light work to a background executor; NC's LevelLightEngine updates inline, so the wrapper only exposes it
public sealed class ThreadedLevelLightEngine
{
    public ThreadedLevelLightEngine(LevelLightEngine inner) => Inner = inner;

    public LevelLightEngine Inner { get; }

    //RunUpdates drains pending light work, maps to vanilla tryScheduleUpdate
    public void RunUpdates() { }

    //GetLightSection the light data of a section
    public DataLayer? GetLightSection(int sectionY, bool sky) => null;
}
