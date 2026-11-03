using NacreCraft.Network.Chat;

namespace NacreCraft.Client.Gui.Narration;

//NarrationThunk lazily resolves narration text so widgets avoid building strings every frame, maps to vanilla NarrationThunk
public sealed class NarrationThunk : IEquatable<NarrationThunk>
{
    public static readonly NarrationThunk Empty = new([]);

    private readonly IReadOnlyList<string> _lines;

    private NarrationThunk(IReadOnlyList<string> lines) => _lines = lines;

    public static NarrationThunk From(string text) => new([text]);

    public static NarrationThunk From(Component text) => new([text.GetString()]);

    public static NarrationThunk From(IReadOnlyList<Component> lines)
    {
        var resolved = new string[lines.Count];
        for (var i = 0; i < lines.Count; i++) resolved[i] = lines[i].GetString();
        return new NarrationThunk(resolved);
    }

    public void GetText(Action<string> output)
    {
        foreach (var line in _lines) output(line);
    }

    public bool Equals(NarrationThunk? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (_lines.Count != other._lines.Count) return false;
        for (var i = 0; i < _lines.Count; i++)
            if (!string.Equals(_lines[i], other._lines[i], StringComparison.Ordinal)) return false;
        return true;
    }

    public override bool Equals(object? obj) => obj is NarrationThunk other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var line in _lines) hash.Add(line, StringComparer.Ordinal);
        return hash.ToHashCode();
    }
}
