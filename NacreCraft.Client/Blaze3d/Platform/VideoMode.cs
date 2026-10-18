namespace NacreCraft.Client.Blaze3d.Platform;

//VideoMode a fullscreen video mode, maps to vanilla com.mojang.blaze3d.platform.VideoMode
//It is stored in options as "1920x1080@60" and matched against the modes the monitor offers
public readonly record struct VideoMode(int Width, int Height, int RefreshRate)
{
    //Write the options-file form
    public string Write() => $"{Width}x{Height}@{RefreshRate}";

    //Read parses the options-file form, null when the text is absent or malformed
    public static VideoMode? Read(string? text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        var parts = text.Split('x', '@');
        if (parts.Length != 3) return null;
        if (!int.TryParse(parts[0], out var width)) return null;
        if (!int.TryParse(parts[1], out var height)) return null;
        if (!int.TryParse(parts[2], out var refreshRate)) return null;
        if (width <= 0 || height <= 0 || refreshRate <= 0) return null;
        return new VideoMode(width, height, refreshRate);
    }
}
