using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Util;

namespace NacreCraft.Game.Server.Progress;

//LevelLoadProgressTracker weights the tracked stages into one 0..1 value, maps to vanilla LevelLoadProgressTracker
//Server startup counts as a fixed weight, the initial chunk stage takes the chunk count and the player stage a fixed 7x7 expectation
public sealed class LevelLoadProgressTracker : LevelLoadListener
{
    private const int PrepareServerWeight = 10;
    private static readonly int ExpectedPlayerChunks = Mth.Square(7);

    private readonly bool _includePlayerChunks;
    private int _totalWeight;
    private int _finalizedWeight;
    private int _segmentWeight;
    private float _segmentFraction;
    private volatile float _progress;

    public LevelLoadProgressTracker(bool includePlayerChunks) => _includePlayerChunks = includePlayerChunks;

    //Get the current progress in 0..1, maps to vanilla get
    public float Get() => _progress;

    public void Start(LevelLoadListener.Stage stage, int totalChunks)
    {
        if (!TracksStage(stage)) return;
        switch (stage)
        {
            case LevelLoadListener.Stage.LoadInitialChunks:
                var playerChunksWeight = _includePlayerChunks ? ExpectedPlayerChunks : 0;
                _totalWeight = PrepareServerWeight + totalChunks + playerChunksWeight;
                //Server startup is already done by the time the initial chunks begin
                BeginSegment(PrepareServerWeight);
                FinishSegment();
                BeginSegment(totalChunks);
                break;
            case LevelLoadListener.Stage.LoadPlayerChunks:
                BeginSegment(ExpectedPlayerChunks);
                break;
        }
    }

    public void Update(LevelLoadListener.Stage stage, int currentChunks, int totalChunks)
    {
        if (!TracksStage(stage)) return;
        _segmentFraction = totalChunks == 0 ? 0f : (float)currentChunks / totalChunks;
        UpdateProgress();
    }

    public void Finish(LevelLoadListener.Stage stage)
    {
        if (TracksStage(stage)) FinishSegment();
    }

    public void UpdateFocus(ResourceKey<Level> dimension, ChunkPos chunkPos)
    {
    }

    private void BeginSegment(int weight)
    {
        _segmentWeight = weight;
        _segmentFraction = 0f;
        UpdateProgress();
    }

    private void FinishSegment()
    {
        _finalizedWeight += _segmentWeight;
        _segmentWeight = 0;
        UpdateProgress();
    }

    //TracksStage only the initial and player stages carry progress, maps to vanilla tracksStage
    private bool TracksStage(LevelLoadListener.Stage stage)
        => stage == LevelLoadListener.Stage.LoadInitialChunks
           || (stage == LevelLoadListener.Stage.LoadPlayerChunks && _includePlayerChunks);

    private void UpdateProgress()
    {
        _progress = _totalWeight == 0
            ? 0f
            : (_finalizedWeight + _segmentFraction * _segmentWeight) / _totalWeight;
    }
}
