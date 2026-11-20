using NacreCraft.Client.Blaze3d.Pipeline;

namespace NacreCraft.Client.Render.Rendertype;

//OutputTarget selects the render target a render type draws into, maps to vanilla net.minecraft.client.renderer.rendertype.OutputTarget
//Vanilla reaches Minecraft.gameRenderer/levelRenderer; NC registers providers because those owner objects are still being ported
public sealed class OutputTarget
{
    private static Func<RenderTarget?> _mainTargetProvider = static () => null;
    private static Func<RenderTarget?> _outlineTargetProvider = static () => null;
    private static Func<RenderTarget?> _weatherTargetProvider = static () => null;
    private static Func<RenderTarget?> _itemEntityTargetProvider = static () => null;

    private readonly string _name;
    private readonly Func<RenderTarget?> _renderTargetSupplier;

    public static readonly OutputTarget MAIN_TARGET = new("main_target", () => _mainTargetProvider());
    public static readonly OutputTarget OUTLINE_TARGET = new("outline_target", () => _outlineTargetProvider());
    public static readonly OutputTarget WEATHER_TARGET = new("weather_target", () => _weatherTargetProvider());
    public static readonly OutputTarget ITEM_ENTITY_TARGET = new("item_entity_target", () => _itemEntityTargetProvider());

    public OutputTarget(string name, Func<RenderTarget?> renderTargetSupplier)
    {
        _name = name;
        _renderTargetSupplier = renderTargetSupplier;
    }

    //SetMainTargetProvider installs the game renderer's main target once that renderer exists
    public static void SetMainTargetProvider(Func<RenderTarget?> provider) => _mainTargetProvider = provider;

    public static void SetOutlineTargetProvider(Func<RenderTarget?> provider) => _outlineTargetProvider = provider;

    public static void SetWeatherTargetProvider(Func<RenderTarget?> provider) => _weatherTargetProvider = provider;

    public static void SetItemEntityTargetProvider(Func<RenderTarget?> provider) => _itemEntityTargetProvider = provider;

    public RenderTarget GetRenderTarget()
        => _renderTargetSupplier() ?? _mainTargetProvider()
           ?? throw new InvalidOperationException($"No render target registered for {this}");

    public override string ToString() => $"OutputTarget[{_name}]";
}
