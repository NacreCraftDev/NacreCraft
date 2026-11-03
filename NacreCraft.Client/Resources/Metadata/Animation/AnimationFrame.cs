using System.Text.Json;

namespace NacreCraft.Client.Resources.Metadata.Animation;

//AnimationFrame one frame of an animation strip, maps to vanilla AnimationFrame
//Time is the duration in ticks, null means the section default
public sealed record AnimationFrame(int Index, int? Time)
{
    public AnimationFrame(int index) : this(index, null) { }

    public int TimeOr(int defaultFrameTime) => Time ?? defaultFrameTime;

    //Parse accepts either a bare index or the full {"index":0,"time":2} form, maps to vanilla either codec
    public static AnimationFrame Parse(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number)
            return new AnimationFrame(Math.Max(0, element.GetInt32()));
        var index = element.TryGetProperty("index", out var indexElement) ? Math.Max(0, indexElement.GetInt32()) : 0;
        int? time = element.TryGetProperty("time", out var timeElement) && timeElement.GetInt32() > 0
            ? timeElement.GetInt32()
            : null;
        return new AnimationFrame(index, time);
    }
}
