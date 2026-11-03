namespace NacreCraft.Client.Gui.Narration;

//NarrationSupplier widgets that describe themselves for screen readers, maps to vanilla NarrationSupplier
public interface NarrationSupplier
{
    void UpdateNarration(NarrationElementOutput output);
}
