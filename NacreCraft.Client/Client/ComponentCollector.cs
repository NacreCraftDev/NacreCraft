using NacreCraft.Network.Chat;

namespace NacreCraft.Client;

//ComponentCollector accumulates formatted text parts and composes them, maps to vanilla net.minecraft.client.ComponentCollector
public sealed class ComponentCollector
{
    private readonly List<FormattedText> _parts = new();

    public void Append(FormattedText component) => _parts.Add(component);

    public FormattedText? GetResult()
    {
        if (_parts.Count == 0) return null;
        if (_parts.Count == 1) return _parts[0];
        return FormattedText.Composite(_parts);
    }

    public FormattedText GetResultOrEmpty() => GetResult() ?? FormattedText.EMPTY;

    public void Reset() => _parts.Clear();
}
