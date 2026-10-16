namespace NacreCraft.Client.Gui;

//IGuiOverlay overlay layer drawn above the current screen, maps to vanilla net.minecraft.client.gui.screens.Overlay
//ScreenManager ticks it with the screen and draws it after the screen's foreground, so the overlay stays on top no
//matter what the screen below does; it removes itself once IsFinished reports true
public interface IGuiOverlay
{
    //Tick advances the fade animation and whatever work the overlay drives; runs on the Tick thread
    void Tick();

    //Render draws over the already rendered screen; runs on the Tick thread while recording the frame
    void Render(IGuiRenderContext context);

    //IsFinished true once the overlay has faded out completely and should be dropped
    bool IsFinished { get; }
}
