using NacreCraft.Client.Gui.Components;

namespace NacreCraft.Client.Gui.Narration;

//NarratableEntry a widget that supplies narration, maps to vanilla NarratableEntry
public interface NarratableEntry : NarrationSupplier, TabOrderedElement
{
    NarrationPriority GetNarrationPriority();

    bool IsActive() => true;

    IEnumerable<NarratableEntry> GetNarratables() => [this];

    //NarrationPriority how urgently a widget wants to be spoken, maps to vanilla NarrationPriority
    public enum NarrationPriority
    {
        None,
        Hovered,
        Focused
    }
}

public static class NarrationPriorityExtensions
{
    //IsTerminal focused entries stop the narration walk
    public static bool IsTerminal(this NarratableEntry.NarrationPriority priority)
        => priority == NarratableEntry.NarrationPriority.Focused;
}
