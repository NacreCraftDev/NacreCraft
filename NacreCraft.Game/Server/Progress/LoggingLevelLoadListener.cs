using NacreCraft.Logging;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Util;

namespace NacreCraft.Game.Server.Progress;

//LoggingLevelLoadListener writes the loading phases to the server log, maps to vanilla LoggingLevelLoadListener
public sealed class LoggingLevelLoadListener : LevelLoadListener
{
    private readonly bool _includePlayerChunks;
    private readonly LevelLoadProgressTracker _progressTracker;
    private bool _closed;
    private long _startTime = long.MaxValue;
    private long _nextLogTime = long.MaxValue;

    public LoggingLevelLoadListener(bool includePlayerChunks)
    {
        _includePlayerChunks = includePlayerChunks;
        _progressTracker = new LevelLoadProgressTracker(includePlayerChunks);
    }

    //ForDedicatedServer logs the initial chunk stage only, maps to vanilla forDedicatedServer
    public static LoggingLevelLoadListener ForDedicatedServer() => new(false);

    //ForSingleplayer also logs the player chunk stage, maps to vanilla forSingleplayer
    public static LoggingLevelLoadListener ForSingleplayer() => new(true);

    public void Start(LevelLoadListener.Stage stage, int totalChunks)
    {
        if (_closed) return;
        if (_startTime == long.MaxValue)
        {
            var now = Environment.TickCount64;
            _startTime = now;
            _nextLogTime = now;
        }
        _progressTracker.Start(stage, totalChunks);
        switch (stage)
        {
            case LevelLoadListener.Stage.PrepareGlobalSpawn:
                Log.Info("Selecting global world spawn...");
                break;
            case LevelLoadListener.Stage.LoadInitialChunks:
                Log.Info($"Loading {totalChunks} persistent chunks...");
                break;
            case LevelLoadListener.Stage.LoadPlayerChunks:
                Log.Info($"Loading {totalChunks} chunks for player spawn...");
                break;
        }
    }

    public void Update(LevelLoadListener.Stage stage, int currentChunks, int totalChunks)
    {
        if (_closed) return;
        _progressTracker.Update(stage, currentChunks, totalChunks);
        var now = Environment.TickCount64;
        if (now <= _nextLogTime) return;
        _nextLogTime += 500;
        var percent = Mth.Floor(_progressTracker.Get() * 100f);
        Log.Info($"Preparing spawn area: {percent}%");
    }

    public void Finish(LevelLoadListener.Stage stage)
    {
        if (_closed) return;
        _progressTracker.Finish(stage);
        var finalStage = _includePlayerChunks
            ? LevelLoadListener.Stage.LoadPlayerChunks
            : LevelLoadListener.Stage.LoadInitialChunks;
        if (stage != finalStage) return;
        Log.Info($"Time elapsed: {Environment.TickCount64 - _startTime} ms");
        _nextLogTime = long.MaxValue;
        _closed = true;
    }

    public void UpdateFocus(ResourceKey<Level> dimension, ChunkPos chunkPos)
    {
    }
}
