using NacreCraft.Primitives;
using NacreCraft.Registry;

namespace NacreCraft.Game.Server.Progress;

//LevelLoadListener reports the level loading phases, maps to vanilla net.minecraft.server.level.progress.LevelLoadListener
public interface LevelLoadListener
{
    //Stage the loading phases, maps to vanilla LevelLoadListener.Stage
    public enum Stage
    {
        StartServer,
        PrepareGlobalSpawn,
        LoadInitialChunks,
        LoadPlayerChunks
    }

    //Start begins a stage with the total chunk count of that stage
    void Start(Stage stage, int totalChunks);

    //Update reports the chunks finished so far within a stage
    void Update(Stage stage, int currentChunks, int totalChunks);

    //Finish ends a stage
    void Finish(Stage stage);

    //UpdateFocus moves the view centre, for viewers that draw the map of chunks being loaded
    void UpdateFocus(ResourceKey<Level> dimension, ChunkPos chunkPos);

    //Compose fans every callback out to both listeners in order, maps to vanilla compose
    public static LevelLoadListener Compose(LevelLoadListener first, LevelLoadListener second)
        => new ComposedLevelLoadListener(first, second);
}

//ComposedLevelLoadListener forwards each callback to two listeners, maps to the anonymous listener of vanilla compose
internal sealed class ComposedLevelLoadListener(LevelLoadListener first, LevelLoadListener second) : LevelLoadListener
{
    public void Start(LevelLoadListener.Stage stage, int totalChunks)
    {
        first.Start(stage, totalChunks);
        second.Start(stage, totalChunks);
    }

    public void Update(LevelLoadListener.Stage stage, int currentChunks, int totalChunks)
    {
        first.Update(stage, currentChunks, totalChunks);
        second.Update(stage, currentChunks, totalChunks);
    }

    public void Finish(LevelLoadListener.Stage stage)
    {
        first.Finish(stage);
        second.Finish(stage);
    }

    public void UpdateFocus(ResourceKey<Level> dimension, ChunkPos chunkPos)
    {
        first.UpdateFocus(dimension, chunkPos);
        second.UpdateFocus(dimension, chunkPos);
    }
}
