using NacreCraft.Logging;

namespace NacreCraft.Client.Gui.Screens;

//LoadingOverlay loading overlay, maps to vanilla net.minecraft.client.gui.screens.LoadingOverlay
//Covers the screen with the brand color, the studio logo and a white progress bar while the stages it was given run
//one per tick. Startup shows it opaque from the first frame; only a manual reload asks for the 500ms fade in, which
//is the same split vanilla makes. Once every stage finished and the overlay has been up for a second it fades out
//over 1000ms so the screen underneath (already initialized by then) shows through
public sealed class LoadingOverlay : IGuiOverlay
{
    //BrandBackground the red brand color vanilla uses when the dark Mojang background option is off
    private static readonly GuiColor BrandBackground = GuiColor.FromRgb(239, 50, 61);
    private const long FadeInMillis = 500;
    private const long FadeOutMillis = 1000;
    //The fade out start waits for the fade in to have been on screen for this long, matching vanilla isReadyToFadeOut
    private const long MinimumCoverMillis = 1000;
    //The displayed progress trails the real one, which is how vanilla keeps the bar from jumping between stages
    private const float Smoothing = 0.95f;
    //The logo texture stacks the left half above the right half, so each blit takes one half of the image
    private const int LogoSourceWidth = 512;
    private const int LogoSourceHeight = 256;

    private readonly GuiWindow _window;
    //The window background in use before the overlay took it over, handed back once the overlay is done
    private readonly GuiColor _previousBackground;
    //A stage reports its own progress from 0 to 1 and is done once it reaches 1; a stage backed by background work or
    //by a running reload keeps the bar honest instead of jumping when the whole build lands
    private readonly List<(string Label, Func<float> Work)> _stages;
    private readonly System.Action? _onFinished;
    private readonly bool _fadeIn;
    //The fade in starts on the first rendered frame rather than at construction: the stages can block the tick thread
    //for a while before anything is drawn, and starting earlier would make the fade in invisible
    private long _fadeInStart = -1;
    //The texture id is resolved lazily: the logo can only be registered once the GPU app exists
    private readonly Func<int> _logoTexture;

    private long _fadeOutStart = -1;
    private float _currentProgress;
    private int _stageIndex;
    //Progress of the stage currently running, 0 to 1
    private float _stageProgress;

    public LoadingOverlay(GuiWindow window, Func<int> logoTexture,
        List<(string Label, Func<float> Work)> stages, System.Action? onFinished = null, bool fadeIn = true)
    {
        _window = window;
        _logoTexture = logoTexture;
        _stages = stages;
        _onFinished = onFinished;
        _fadeIn = fadeIn;
        //The window background is what the render thread clears to until the tick thread publishes its first snapshot,
        //and that gap is the slowest part of startup because the glyphs have to be baked first
        //Adopting the brand colour here keeps that gap from flashing the default dark grey, which vanilla never shows
        _previousBackground = window.BackgroundColor;
        window.BackgroundColor = BrandBackground;
    }

    public bool IsFinished { get; private set; }

    public void Tick()
    {
        if (_fadeOutStart > 0) return;
        //The current stage is polled until it reports 1, then the next one starts on the following tick
        if (_stageIndex < _stages.Count)
        {
            var stage = _stages[_stageIndex];
            try
            {
                _stageProgress = Math.Clamp(stage.Work(), 0f, 1f);
            }
            catch (Exception ex)
            {
                //A failed startup stage must not leave the client stuck on the loading screen forever
                Log.Warning($"Loading stage failed {stage.Label} {ex.Message}");
                _stageProgress = 1f;
            }
            if (_stageProgress >= 1f)
            {
                _stageIndex++;
                _stageProgress = 0f;
            }
            return;
        }
        if (!IsReadyToFadeOut()) return;
        _onFinished?.Invoke();
        _fadeOutStart = Environment.TickCount64;
    }

    //IsReadyToFadeOut waits for the fade in to have been visible for a moment before fading out, maps to vanilla
    //Before the first frame is drawn there is nothing to hold back, so it reports false
    private bool IsReadyToFadeOut()
        => !_fadeIn || _fadeInStart > 0 && Environment.TickCount64 - _fadeInStart >= MinimumCoverMillis;

    public void Render(IGuiRenderContext context)
    {
        var width = _window.ScaledWidth;
        var height = _window.ScaledHeight;
        var now = Environment.TickCount64;
        if (_fadeIn && _fadeInStart < 0) _fadeInStart = now;
        var fadeInAnim = _fadeIn && _fadeInStart > 0 ? (now - _fadeInStart) / (float)FadeInMillis : 1f;
        var fadeOutAnim = _fadeOutStart > 0 ? (now - _fadeOutStart) / (float)FadeOutMillis : -1f;

        float backgroundAlpha;
        float logoAlpha;
        if (fadeOutAnim >= 1f)
        {
            //Past a full fade out only the brand layer is left, thinning out until the screen below is fully visible
            backgroundAlpha = 1f - Math.Clamp(fadeOutAnim - 1f, 0f, 1f);
            logoAlpha = backgroundAlpha;
            if (fadeOutAnim >= 2f)
            {
                IsFinished = true;
                //The overlay is about to be removed from the render chain, so give the window background back
                _window.BackgroundColor = _previousBackground;
            }
        }
        else if (fadeOutAnim >= 0f)
        {
            backgroundAlpha = 1f;
            logoAlpha = 1f;
        }
        else
        {
            //Vanilla never drops the cover below 0.15 alpha while fading in, so the logo does not flash on a bright screen
            backgroundAlpha = Math.Clamp(fadeInAnim, 0.15f, 1f);
            logoAlpha = Math.Clamp(fadeInAnim, 0f, 1f);
        }

        context.DrawQuad(0, 0, width, height, BrandBackground with { A = backgroundAlpha });

        //Logo: a quarter of the smaller dimension tall, four times as wide, split across the two halves of the texture
        var logoHeight = (int)(Math.Min(width * 0.75f, height) * 0.25f);
        var halfWidth = logoHeight * 2;
        var centerX = width / 2;
        var logoY = height / 2 - logoHeight / 2;
        var logoColor = GuiColor.White with { A = logoAlpha };
        var textureId = _logoTexture();
        if (textureId != 0 && logoHeight > 0)
        {
            context.DrawImage(textureId, centerX - halfWidth, logoY, halfWidth, logoHeight, 0, 0, LogoSourceWidth, LogoSourceHeight, logoColor);
            context.DrawImage(textureId, centerX, logoY, halfWidth, logoHeight, 0, LogoSourceHeight, LogoSourceWidth, LogoSourceHeight, logoColor);
        }

        //The bar trails the real progress in the same way vanilla smooths it; finished stages plus the running one
        var actualProgress = _stages.Count == 0 ? 1f : (_stageIndex + _stageProgress) / _stages.Count;
        _currentProgress = Math.Clamp(_currentProgress * Smoothing + actualProgress * (1f - Smoothing), 0f, 1f);
        if (fadeOutAnim >= 1f) return;
        var barY = (int)(height * 0.8325f);
        DrawProgressBar(context, centerX - halfWidth, barY - 5, centerX + halfWidth, barY + 5,
            1f - Math.Clamp(fadeOutAnim, 0f, 1f));
    }

    //DrawProgressBar draws the white frame and the filled part, maps to vanilla extractProgressBar
    private void DrawProgressBar(IGuiRenderContext context, int x0, int y0, int x1, int y1, float fade)
    {
        var filled = (int)MathF.Ceiling((x1 - x0 - 2) * _currentProgress);
        var color = GuiColor.White with { A = fade };
        var barHeight = y1 - y0;
        context.DrawQuad(x0 + 2, y0 + 2, filled, barHeight - 4, color);
        context.DrawQuad(x0 + 1, y0, x1 - x0 - 2, 1, color);
        context.DrawQuad(x0 + 1, y1 - 1, x1 - x0 - 2, 1, color);
        context.DrawQuad(x0, y0, 1, barHeight, color);
        context.DrawQuad(x1 - 1, y0, 1, barHeight, color);
    }
}
