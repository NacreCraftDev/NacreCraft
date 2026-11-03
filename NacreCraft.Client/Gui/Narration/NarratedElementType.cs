namespace NacreCraft.Client.Gui.Narration;

//NarratedElementType kinds of narration text a widget contributes, maps to vanilla NarratedElementType
//Declaration order matters, the collector sorts entries by it
public enum NarratedElementType
{
    Title,
    Position,
    Hint,
    Usage
}
