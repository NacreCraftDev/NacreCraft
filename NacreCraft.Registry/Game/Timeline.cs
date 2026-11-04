using NacreCraft.Registry.Environment;

namespace NacreCraft.Registry;

//Timeline timeline, maps to vanilla net.minecraft.world.timeline.Timeline
//Divides a world clock into periods (periodTicks) and defines the time markers within a period
//Attribute tracks drive client environment sampling; C# cannot express EnvironmentAttribute<?>, so the track table holds objects
//and CreateTrackSampler casts the entry back to the attribute's Value type
public sealed class Timeline
{
    //TimeMarkerInfo marker definition; ticks is the position within the period and showInCommands controls command suggestion visibility
    public sealed record TimeMarkerInfo(int Ticks, bool ShowInCommands);

    private readonly Dictionary<ResourceKey<ClockTimeMarker>, TimeMarkerInfo> _timeMarkers;
    //Key is the EnvironmentAttribute<Value> instance, value is the matching AttributeTrack<Value>
    private readonly Dictionary<object, object> _tracks;

    public Timeline(Holder<WorldClock> clock, int? periodTicks,
        IEnumerable<KeyValuePair<ResourceKey<ClockTimeMarker>, TimeMarkerInfo>>? timeMarkers = null,
        IEnumerable<KeyValuePair<object, object>>? tracks = null)
    {
        Clock = clock;
        PeriodTicks = periodTicks;
        _timeMarkers = timeMarkers is null
            ? new Dictionary<ResourceKey<ClockTimeMarker>, TimeMarkerInfo>()
            : new Dictionary<ResourceKey<ClockTimeMarker>, TimeMarkerInfo>(timeMarkers);
        TimeMarkers = _timeMarkers;
        _tracks = tracks is null ? new Dictionary<object, object>() : new Dictionary<object, object>(tracks);
    }

    //Clock the owning world clock
    public Holder<WorldClock> Clock { get; }

    //PeriodTicks period tick count; null means an aperiodic timeline
    public int? PeriodTicks { get; }

    //TimeMarkers read-only view of the time marker definitions within the period
    public IReadOnlyDictionary<ResourceKey<ClockTimeMarker>, TimeMarkerInfo> TimeMarkers { get; }

    //GetPeriodCount number of complete periods elapsed; returns 0 for an aperiodic timeline
    public int GetPeriodCount(ClockManager clockManager)
    {
        if (PeriodTicks is null)
            return 0;
        return (int)(GetTotalTicks(clockManager) / PeriodTicks.Value);
    }

    //GetCurrentTicks ticks within the current period; returns total ticks for an aperiodic timeline
    public long GetCurrentTicks(ClockManager clockManager)
    {
        var totalTicks = GetTotalTicks(clockManager);
        return PeriodTicks is null ? totalTicks : totalTicks % PeriodTicks.Value;
    }

    public long GetTotalTicks(ClockManager clockManager) => clockManager.GetTotalTicks(Clock);

    //RegisterTimeMarkers expands the markers within the period into ClockTimeMarkers and hands them to the callback to register
    public void RegisterTimeMarkers(Action<ResourceKey<ClockTimeMarker>, ClockTimeMarker> output)
    {
        foreach (var (key, info) in _timeMarkers)
            output(key, new ClockTimeMarker(Clock, info.Ticks, PeriodTicks, info.ShowInCommands));
    }

    //Attributes the attributes this timeline has a track for, maps to vanilla Timeline.attributes
    public IReadOnlyCollection<object> Attributes => _tracks.Keys;

    //HasTrack whether the timeline carries a track for the attribute
    public bool HasTrack(IEnvironmentAttribute attribute) => _tracks.ContainsKey(attribute);

    //CreateTrackSampler bakes the track bound to one attribute, maps to vanilla Timeline.createTrackSampler
    public AttributeTrackSampler<Value> CreateTrackSampler<Value>(EnvironmentAttribute<Value> attribute, ClockManager clockManager)
    {
        if (!_tracks.TryGetValue(attribute, out var raw) || raw is not AttributeTrack<Value> track)
            throw new InvalidOperationException($"Timeline has no track for {attribute}");
        return track.BakeSampler(attribute, Clock, PeriodTicks, clockManager);
    }
}
