using System.Text.Json;

namespace NacreCraft.Client.Resources.Metadata.Animation;

//AnimationMetadataSection parses the animation section of .mcmeta, maps to vanilla AnimationMetadataSection
//Root structure {"animation":{"frametime":2,"interpolate":true,"width":16,"height":16,"frames":[...]}}
public sealed record AnimationMetadataSection(
    IReadOnlyList<AnimationFrame>? Frames,
    int? FrameWidth,
    int? FrameHeight,
    int DefaultFrameTime,
    bool InterpolatedFrames)
{
    //VanillaDefaultFrameTime vanilla default when frametime is absent
    public const int VanillaDefaultFrameTime = 1;

    //Parse returns null when the root has no animation section
    public static AnimationMetadataSection? Parse(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("animation", out var animation))
            return null;
        if (animation.ValueKind != JsonValueKind.Object)
            return new AnimationMetadataSection(null, null, null, VanillaDefaultFrameTime, false);

        List<AnimationFrame>? frames = null;
        if (animation.TryGetProperty("frames", out var framesElement) && framesElement.ValueKind == JsonValueKind.Array)
        {
            frames = new List<AnimationFrame>(framesElement.GetArrayLength());
            foreach (var frameElement in framesElement.EnumerateArray())
                frames.Add(AnimationFrame.Parse(frameElement));
        }

        return new AnimationMetadataSection(
            frames,
            ReadPositive(animation, "width"),
            ReadPositive(animation, "height"),
            ReadPositive(animation, "frametime") ?? VanillaDefaultFrameTime,
            animation.TryGetProperty("interpolate", out var interpolateElement) && interpolateElement.ValueKind == JsonValueKind.True);
    }

    //CalculateFrameSize derives the frame dimensions from the strip size, maps to vanilla calculateFrameSize
    public FrameSize CalculateFrameSize(int spriteWidth, int spriteHeight)
    {
        if (FrameWidth is { } width)
            return FrameHeight is { } height ? new FrameSize(width, height) : new FrameSize(width, spriteHeight);
        if (FrameHeight is { } onlyHeight)
            return new FrameSize(spriteWidth, onlyHeight);
        var minDimension = Math.Min(spriteWidth, spriteHeight);
        return new FrameSize(minDimension, minDimension);
    }

    private static int? ReadPositive(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.GetInt32() > 0 ? value.GetInt32() : null;
}
