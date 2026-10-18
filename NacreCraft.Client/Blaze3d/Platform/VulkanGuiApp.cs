using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using NacreCraft.Client.Blaze3d;
using NacreCraft.Client.Blaze3d.Buffers;
using NacreCraft.Client.Blaze3d.Pipeline;
using NacreCraft.Client.Blaze3d.Systems;
using NacreCraft.Client.Blaze3d.Textures;
using NacreCraft.Client.Blaze3d.Vulkan;
using NacreCraft.Client.Gui;
using NacreCraft.Client.Gui.Font;
using NacreCraft.Client.Gui.Render;
using NacreCraft.Client.Gui.Render.Pip;
using NacreCraft.Client.Gui.Render.State;
using NacreCraft.Client.Render;
using NacreCraft.Client.Render.State.Gui;
using NacreCraft.Client.Render.Texture.Atlas;
using NacreCraft.Logging;
using Silk.NET.Input;
using Silk.NET.Vulkan;
//The Registry type declares a nested Action, so the delegate is aliased explicitly
using Action = System.Action;
//Aliased so the pack resource manager cannot be confused with the GUI texture resource manager
using PackIdentifier = NacreCraft.Registry.Identifier;
using ClientPackType = NacreCraft.Resources.PackType;
using ClientResourceManager = NacreCraft.Resources.ResourceManager;

namespace NacreCraft.Client.Blaze3d.Platform;

//VulkanGuiApp the GUI window app, the entry the game layer drives
//It opens a SilkWay window, runs a 20tps Tick thread that builds GUI snapshots and a Render callback that records them
//The Game layer only talks to it through events and a handful of setters, so the GPU layer stays independent
public sealed class VulkanGuiApp : VulkanAppBase
{
    private GuiRenderState _renderState = null!;
    private GuiRenderer _guiRenderer = null!;
    private GuiRenderContext _renderContext = null!;
    private GuiResourceManager _resourceManager = null!;
    //_guiPipeline the precompiled RenderPipelines.GUI, precompiled so the first frame does not stutter
    private CompiledRenderPipeline _guiPipeline = null!;
    private FontAtlas? _fontAtlas;
    //F7 GlyphFont dynamic bake path GlyphStitcher+GlyphFont replacing FontAtlas
    private GlyphStitcher? _glyphStitcher;
    private GlyphFont? _font;
    //P0 GuiSpriteManager caches GuiSprites by identifier, lazily loading PNG+.mcmeta
    //On swapchain recreation ClearCache invalidates old TextureSetups
    private GuiSpriteManager? _spriteManager;
    //GuiAtlas the stitched GUI sprite atlas; it only depends on pack contents, so it survives swapchain recreation
    //and is rebuilt only on a resource reload
    private GuiAtlas? _guiAtlas;
    //The client pack resource manager GUI textures and fonts are read through, so a resource pack can override them
    private readonly ClientResourceManager? _clientResources;
    //The logging abstraction decoupled from NacreCraft.Logging, passed to GuiResourceManager
    private readonly IGpuLogger _logger;
    //Input event queue Silk callback threads enqueue; the render thread calls PollInput to dequeue and dispatch
    private readonly ConcurrentQueue<InputEvent> _inputQueue = new();
    //IInputContext the input context returned by CreateInput, released on Dispose
    private IInputContext? _inputContext;
    //_latestSnapshot written on the Render thread by SubmitFrame immediately before it is consumed
    //Stage 7 Tick/Render decoupling; the tick thread no longer touches the GUI scene at all
    private volatile GuiRenderState? _latestSnapshot;
    //_lastGuiTimestamp previous render-thread frame timestamp, used to derive the widget delta
    private long _lastGuiTimestamp;
    //_resourceLock protects _resourceManager/_renderContext replacement and keeps references stable during submission
    private readonly object _resourceLock = new();
    //_diagFrame counts rendered frames for the throttled frame logging
    private int _submitDiag;
    private int _diagFrame;
    //_dumpFrame counts rendered frames for the periodic scene dump
    private int _dumpFrame;

    //Blur post-processing resources: the BeforeBlur segment renders to offscreen, horizontally blurs to temp and vertically blurs to the swapchain
    //The AfterBlur segment uses LoadOp=Load to preserve the blurred background with GUI widgets overlaid
    private GpuTexture? _blurOffscreen;
    private GpuTexture? _blurTemp;
    private GpuTextureView? _blurOffscreenView;
    private GpuTextureView? _blurTempView;
    private GpuSampler? _blurSampler;
    private GpuBuffer? _blurUniformBuffer;
    //BlurRadius sample step radius controlling blur strength; larger is blurrier
    private const float BlurRadius = 2.0f;

    //P15 ItemItemAtlas renders the 3D item atlas into AtlasTexture for Hotbar item icons to sample
    //Independent of swapchain extent and reused across resizes; textureId is re-registered when _resourceManager is rebuilt
    private ItemItemAtlas? _itemAtlas;
    //ItemAtlasTextureId the textureId obtained by registering AtlasTexture with GuiResourceManager, for GameScreen DrawImage
    private int _itemAtlasTextureId;
    //P15 ItemPipRenderer oversized-item PIP offscreen renderer registered with GuiRenderer, dispatched by ItemPipState
    private ItemPipRenderer? _itemPipRenderer;

    //W7 world render resources depth image + ViewProj UBO + atlas/lightmap sampler descriptor set
    //IWorldRenderer injected by the Game layer; VulkanGuiApp schedules Prepare/Upload/Draw and manages GPU resources
    //Using an interface avoids a circular dependency where NacreCraft.Gpu references NacreCraft.Game
    private IWorldRenderer? _levelRenderer;
    private GpuTexture? _depthImage;
    private GpuTextureView? _depthView;
    private GpuBuffer? _worldViewProjBuffer;
    private GpuTexture? _blockAtlasImage;
    private GpuTexture? _lightmapImage;
    private GpuTextureView? _worldBlockAtlasView;
    private GpuSampler? _worldBlockAtlasSampler;
    private GpuTextureView? _worldLightmapView;
    private GpuSampler? _worldLightmapSampler;
    private GpuSampler? _worldSampler;
    //_injectedBlockAtlas/_injectedLightmap real textures injected by the Game layer; when null CreateWorldResources uses placeholders
    //Injected resources are managed by this class and released in OnCleanupPipelineResources; DisposeWorldResources does not release them
    private BlockTextureAtlas? _injectedBlockAtlas;
    private LightTexture? _injectedLightmap;

    //Tick thread state; the Tick thread runs game logic at 20tps and publishes GUI snapshots
    private Thread? _tickThread;
    private volatile bool _tickThreadRunning;
    //_tickException captured on the Tick thread and rethrown by OnAfterRun on the main thread
    private Exception? _tickException;
    //TickTargetInterval target seconds per Tick cycle
    //The loop builds the GUI scene and publishes the snapshot, so its rate is also how often the picture changes; at
    //20tps everything animated stepped 20 times a second, which reads as stutter no matter how fast the renderer runs
    private const double TickTargetInterval = 1.0 / 60.0;

    //Windows rounds sleeps to a ~15.6ms timer tick unless the resolution is raised, which on its own caps this loop
    //near 40 cycles a second; asking for 1ms makes the waits below land where they ask
    [System.Runtime.InteropServices.DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
    private static extern uint TimeBeginPeriod(uint milliseconds);
    //TickJoinTimeoutMs how long OnAfterRun waits for the Tick thread to exit before declaring a deadlock
    private const int TickJoinTimeoutMs = 2000;

    //Whether world rendering is enabled; OnRecordCommandBuffer checks this to decide whether to call the world RenderPass
    public bool WorldRenderEnabled => _levelRenderer is not null;

    //Device exposes the GpuDevice for the Game layer to create GpuBufferPool and other GPU resources on SwapchainRecreated
    //_device is ready when SwapchainRecreated fires; access before construction returns a null-forgiving instance
    public GpuDevice Device => _device;

    //GuiWindow created externally and passed in, letting test code pre-arrange widgets
    public GuiWindow Window { get; }

    //SubmissionCpuMs CPU time SubmitFrame spent building the snapshot
    public double SubmissionCpuMs { get; private set; }
    //RenderCpuMs CPU time OnRecordCommandBuffer spent recording the frame
    public double RenderCpuMs { get; private set; }
    //TickRate actual Tick cycles per second measured each second
    public int TickRate { get; private set; }

    //DrawCallCount/MeshCount/VertexCount passed through from GuiRenderer's same-named fields
    public int DrawCallCount => _guiRenderer?.DrawCallCount ?? 0;
    public int MeshCount => _guiRenderer?.MeshCount ?? 0;
    public int VertexCount => _guiRenderer?.VertexCount ?? 0;
    //PipelineHits/Misses pipeline cache statistics from the device, for the debug overlay
    public int PipelineHits => _vkDevice?.PipelineHits ?? 0;
    public int PipelineMisses => _vkDevice?.PipelineMisses ?? 0;

    //ItemAtlas/ItemAtlasTextureId/ItemPipRenderer exposed for GameScreen to draw hotbar item icons
    public ItemItemAtlas? ItemAtlas => _itemAtlas;
    public int ItemAtlasTextureId => _itemAtlasTextureId;
    public ItemPipRenderer? ItemPipRenderer => _itemPipRenderer;

    //World render statistics for the debug overlay
    public int WorldSectionCount => _levelRenderer?.SectionCount ?? 0;
    public int WorldVisibleSectionCount => _levelRenderer?.VisibleSectionCount ?? 0;
    public int WorldVertexCount => _levelRenderer?.TotalVertexCount ?? 0;
    public int WorldDrawCallCount => _levelRenderer?.DrawCallCount ?? 0;

    protected override string WindowTitle => "NacreCraft.Gpu VulkanGuiApp";

    //Raw input events; the Game layer subscribes so business keys and mouse clicks reach a Screen
    public event System.Action<int>? RawKeyDown;
    public event System.Action<GuiMouseButton, int, int>? RawMouseDown;
    public event System.Action<GuiMouseButton, int, int>? RawMouseUp;
    public event System.Action<int, int>? RawMouseMove;
    //SwapchainRecreated fired after resources are rebuilt, so the Game layer can re-inject textures and re-layout screens
    public event Action? SwapchainRecreated;
    //FrameTick the game logic callback invoked on the Tick thread, world logic only
    public event System.Action<double>? FrameTick;

    //GuiTick invoked on the render thread before the frame is built, so widget hover, layout and animation follow the
    //frame rate; vanilla computes hover inside renderWidget for the same reason
    public event System.Action<double>? GuiTick;

    //SetLevelRenderer injects the world renderer; null disables the world pass
    public void SetLevelRenderer(IWorldRenderer? renderer)
    {
        _levelRenderer = renderer;
    }

    //SetBlockAtlas/SetLightmap feed the real world textures in; without them CreateWorldResources uses placeholders
    public void SetBlockAtlas(BlockTextureAtlas? atlas)
    {
        _injectedBlockAtlas = atlas;
    }

    public void SetLightmap(LightTexture? lightmap)
    {
        _injectedLightmap = lightmap;
    }

    //RegisterTexture loads a PNG by path, registers it as a texture and returns a textureId for GuiImage to reference
    //Delegates to the current _resourceManager; after swapchain recreation _resourceManager is new so textureId may change
    //Screen.Init calls this method again during Resized re-layout to get new ids for GuiImage
    //_resourceManager is null before OnCreatePipelineResources and returns 0, using the placeholder
    //At the end of OnCreatePipelineResources it fires SwapchainRecreated so Screen re-Init gets real textureIds
    //Stage 7 holds _resourceLock to protect the _resourceManager field and inner dictionaries from racing with OnSwapchainRecreated rebuilds
    public int RegisterTexture(PackIdentifier location)
    {
        lock (_resourceLock)
        {
            if (_resourceManager is null || _clientResources is null) return 0;
            var resource = _clientResources.GetResource(ClientPackType.ClientResources, location);
            if (resource is null) return 0;
            using var stream = resource.Open();
            return _resourceManager.RegisterTexture(stream, location.ToString());
        }
    }

    //ReloadResources drops the GUI caches that mirror pack contents, called once a resource pack reload finished
    //maps to the levelExtractor.allChanged vanilla runs after the reload chain completes
    //The GUI atlas is rebuilt here, which is the only time pack contents can change
    public void ReloadResources()
    {
        lock (_resourceLock)
        {
            _guiAtlas?.Dispose();
            _guiAtlas = GuiAtlas.Build(_clientResources, _device, _logger);
            if (_spriteManager is not null)
            {
                _spriteManager.Atlas = _guiAtlas;
                _spriteManager.ClearCache();
            }
        }
    }

    public VulkanGuiApp() : this(true, 800, 600)
    {
    }

    public VulkanGuiApp(int width, int height) : this(true, width, height)
    {
    }

    //enableVsync and startFullscreen are passed by the caller from GameConfig so the GPU layer does not depend on the GameConfig type
    public VulkanGuiApp(bool enableVsync, int width, int height, IGpuLogger? logger = null,
        ClientResourceManager? clientResources = null, bool startFullscreen = false) : base(width, height)
    {
        EnableVsync = enableVsync;
        StartFullscreen = startFullscreen;
        _logger = logger ?? new ConsoleGpuLogger();
        _clientResources = clientResources;
        Window = new GuiWindow(width, height);
    }

    //OnCreatePipelineResources creates the new path's resources
    //_swapchainExtent/_swapchainImageFormat are ready at this point
    //First UpdateSurfaceSize recomputes guiScale, then GuiResourceManager/GuiRenderState/GuiRenderer/GuiRenderContext are created
    //Precompiles the GUI pipeline series to avoid runtime compile stutter
    //At the end it fires SwapchainRecreated to notify Screen to re-Init, when _resourceManager is ready to load textures
    //F7 initializes the Font dynamic bake path GlyphStitcher+FontSet+Font replacing FontAtlas
    protected override void OnCreatePipelineResources()
    {
        try
        {
            CreatePipelineResources();
        }
        catch (Exception ex)
        {
            //A failure here leaves _renderContext null and the app renders nothing but the clear pass,
            //so the cause is logged before it is swallowed upstream
            Log.Debug($"gui resources failed to build: {ex}");
            throw;
        }
    }

    private void CreatePipelineResources()
    {
        var w = (int)_swapchainExtent.Width;
        var h = (int)_swapchainExtent.Height;
        Window.UpdateSurfaceSize(w, h);
        _fontAtlas = FontAtlas.FromSystemFont();
        _resourceManager = new GuiResourceManager(_device, w, h, _fontAtlas, _logger);
        _renderState = new GuiRenderState();
        _guiRenderer = new GuiRenderer();
        //F7 creates the Font dynamic bake path GlyphStitcher, wiring into GuiResourceManager to register the atlas
        _glyphStitcher = new GlyphStitcher(_device, _resourceManager);
        _font = CreateFontFromAssets();
        //P0 creates GuiSpriteManager and injects it into GuiRenderContext so DrawSprite dispatches by .mcmeta
        //The GUI atlas is stitched once per pack set, maps to vanilla AtlasIds.GUI
        _guiAtlas ??= GuiAtlas.Build(_clientResources, _device, _logger);
        _spriteManager = new GuiSpriteManager(_clientResources, _resourceManager, _guiAtlas);
        _renderContext = new GuiRenderContext(_renderState, w, h, Window.GuiScale,
            _font, _resourceManager.FontAtlas, _resourceManager.FontTexture,
            id => _resourceManager.ResolveTexture(id),
            _spriteManager);
        PrecompileGuiPipelines();
        CreateBlurResources(w, h);
        CreateItemAtlasResources();
        //SwapchainRecreated fires first so the Game layer injects real textures+LevelRenderer, then world resources are created
        //The first world resource creation already uses the injected real atlas/lightmap instead of placeholders
        SwapchainRecreated?.Invoke();
        CreateWorldResources(w, h);
        Log.Debug($"gui resources ready context={_renderContext is not null} state={_renderState is not null} font={_font is not null} guiScale={Window.GuiScale}");
    }

    //CreateItemAtlasResources creates ItemItemAtlas+ItemPipRenderer and registers them with GuiRenderer+ResourceManager
    //_itemAtlas/_itemPipRenderer do not depend on swapchain extent and are reused across resizes
    //_itemAtlasTextureId depends on _resourceManager and re-RegisterImages on rebuild
    //ItemItemAtlas is only newed when _itemAtlas is null on first creation; later resizes skip creation and only re-register the textureId
    private void CreateItemAtlasResources()
    {
        if (_itemAtlas is null)
        {
            //512x512 atlas 64x64 slots 8x8=64 slots, matching the VulkanItemAtlasApp test config
            _itemAtlas = new ItemItemAtlas(_device, 512, 64);
            _itemPipRenderer = new ItemPipRenderer(_device);
            _guiRenderer.RegisterPipRenderer(_itemPipRenderer);
        }
        _itemAtlasTextureId = _resourceManager.RegisterImage(_itemAtlas.AtlasTexture);
    }

    //CreateWorldResources creates world render GPU resources depth image + ViewProj UBO + atlas/lightmap sampler
    //blockAtlas the first version uses a 1x1 white placeholder texture; W9 wires the real block atlas; lightmap uses a 16x16 full-bright placeholder
    //On swapchain recreation the old resources are Disposed and rebuilt to match the new extent
    private void CreateWorldResources(int w, int h)
    {
        //Precompiles the 3 terrain pipelines to avoid first-compile stutter in OnRecordCommandBuffer
        _device.PrecompilePipeline(WorldRenderPipelines.SOLID_TERRAIN);
        _device.PrecompilePipeline(WorldRenderPipelines.CUTOUT_TERRAIN);
        _device.PrecompilePipeline(WorldRenderPipelines.TRANSLUCENT_TERRAIN);

        //depth image D32Sfloat DepthAttachment with the same extent as the swapchain
        _depthImage = _device.CreateTexture(null, GpuTexture.UsageRenderAttachment, GpuFormat.D32Float, w, h, 1, 1);
        //First layout transition of the depth image Undefined→DepthStencilAttachmentOptimal
        //Upload does not read pixels for a DepthAttachment and only does the barrier so dynamic rendering sees the expected layout
        _depthImage.Upload(ReadOnlySpan<byte>.Empty);
        _depthView = _device.CreateTextureView(_depthImage);

        //set 0 MATRICES_PROJECTION, bound by the name "Matrices"
        _worldViewProjBuffer = _device.CreateBuffer(null, GpuBuffer.UsageUniform | GpuBuffer.UsageMapWrite, 64);

        //set 1 SAMPLER0_SAMPLER1 atlas + lightmap, bound by the names "Sampler0"/"Sampler1"
        //Placeholder textures share a linear sampler; injected textures bring their own nearest sampler
        _worldSampler = _device.CreateSampler(AddressMode.ClampToEdge, AddressMode.ClampToEdge, FilterMode.Linear, FilterMode.Linear, 1, null);

        //If blockAtlas is injected use the real atlas, otherwise create a 1x1 white RGBA8 placeholder
        GpuTexture blockAtlasImage;
        GpuSampler blockAtlasSampler;
        if (_injectedBlockAtlas is not null && _injectedBlockAtlas.AtlasImage is not null && _injectedBlockAtlas.Sampler is not null)
        {
            blockAtlasImage = _injectedBlockAtlas.AtlasImage;
            blockAtlasSampler = _injectedBlockAtlas.Sampler;
        }
        else
        {
            _blockAtlasImage = _device.CreateTexture(null, GpuTexture.UsageTextureBinding, GpuFormat.Rgba8Unorm, 1, 1, 1, 1);
            _blockAtlasImage.Upload(new byte[] { 255, 255, 255, 255 });
            blockAtlasImage = _blockAtlasImage;
            blockAtlasSampler = _worldSampler;
        }

        //If lightmap is injected use the real LightTexture, otherwise create a 16x16 full-bright RGBA8 placeholder
        GpuTexture lightmapImage;
        GpuSampler lightmapSampler;
        if (_injectedLightmap is not null && _injectedLightmap.Texture is not null && _injectedLightmap.Sampler is not null)
        {
            lightmapImage = _injectedLightmap.Texture;
            lightmapSampler = _injectedLightmap.Sampler;
        }
        else
        {
            _lightmapImage = _device.CreateTexture(null, GpuTexture.UsageTextureBinding, GpuFormat.Rgba8Unorm, 16, 16, 1, 1);
            var lightmapPixels = new byte[16 * 16 * 4];
            for (var i = 0; i < lightmapPixels.Length; i += 4)
            {
                lightmapPixels[i] = 255;
                lightmapPixels[i + 1] = 255;
                lightmapPixels[i + 2] = 255;
                lightmapPixels[i + 3] = 255;
            }
            _lightmapImage.Upload(lightmapPixels);
            lightmapImage = _lightmapImage;
            lightmapSampler = _worldSampler;
        }

        _worldBlockAtlasView = _device.CreateTextureView(blockAtlasImage);
        _worldBlockAtlasSampler = blockAtlasSampler;
        _worldLightmapView = _device.CreateTextureView(lightmapImage);
        _worldLightmapSampler = lightmapSampler;
    }

    //DisposeWorldResources releases the resources created by CreateWorldResources
    //Injected atlas/lightmap are not released here; they belong to the Game layer and are released in OnCleanupPipelineResources
    private void DisposeWorldResources()
    {
        _depthView?.Dispose();
        _depthImage?.Dispose();
        _worldViewProjBuffer?.Dispose();
        _worldBlockAtlasView?.Dispose();
        _worldLightmapView?.Dispose();
        _blockAtlasImage?.Dispose();
        _lightmapImage?.Dispose();
        _worldSampler?.Dispose();
        _depthView = null;
        _depthImage = null;
        _worldViewProjBuffer = null;
        _worldBlockAtlasView = null;
        _worldBlockAtlasSampler = null;
        _worldLightmapView = null;
        _worldLightmapSampler = null;
        _blockAtlasImage = null;
        _lightmapImage = null;
        _worldSampler = null;
    }

    //CreateFontFromAssets builds the GlyphFont from the assets font config
    //Returns null when the assets directory or the font json is missing, so DrawText falls back to FontAtlas
    private GlyphFont? CreateFontFromAssets()
    {
        if (_clientResources is null)
        {
            _logger.Warning("no client resource manager; GlyphFont initialization falls back to FontAtlas");
            return null;
        }
        var accessor = new ResourceFontAccessor(_clientResources);
        //Vanilla loads minecraft:default as the game font (Minecraft.DEFAULT_FONT); alt is only the SGA variant
        //used by the enchanting table language
        var fontId = "minecraft:default";
        var parts = fontId.Split(':', 2);
        var (namespaceId, fontPath) = parts.Length <= 1 ? ("minecraft", fontId) : (parts[0], parts[1]);
        var fontConfigPath = $"{namespaceId}:font/{fontPath}.json";
        var stream = accessor.OpenResource(fontConfigPath);
        if (stream is null)
        {
            _logger.Warning($"font config does not exist {fontConfigPath}; GlyphFont initialization falls back to FontAtlas");
            return null;
        }
        List<IGlyphProvider.Conditional> providers;
        using (stream)
        {
            providers = FontProviderDefinitionLoader.Load(stream, accessor);
        }
        var fontSet = new FontSet();
        fontSet.Reload(providers, new HashSet<FontOption>());
        //A provider chain that came out empty or inactive leaves every codepoint on the missing glyph, which looks
        //like a working text layout with wrong shapes, so the chain summary is logged
        foreach (var provider in fontSet.ActiveProviders)
            Log.Debug($"font {fontId} provider glyphs={provider.GetSupportedGlyphs().Count}");
        Log.Debug($"font {fontId} providers={providers.Count} active={fontSet.ActiveProviders.Count}");
        //7/9 are the vanilla ascent/descent the font bakes glyphs against
        return new GlyphFont(fontSet, _glyphStitcher!, 7, 9);
    }

    //PrecompileGuiPipelines compiles the whole GUI pipeline series up front to avoid first-use stutter
    private void PrecompileGuiPipelines()
    {
        _guiPipeline = _device.PrecompilePipeline(RenderPipelines.GUI);
        _device.PrecompilePipeline(RenderPipelines.GUI_INVERT);
        _device.PrecompilePipeline(RenderPipelines.GUI_TEXT);
        _device.PrecompilePipeline(RenderPipelines.GUI_TEXTURED);
        _device.PrecompilePipeline(RenderPipelines.GUI_TEXT_GRAYSCALE);
        _device.PrecompilePipeline(RenderPipelines.GUI_TEXT_SEE_THROUGH);
        _device.PrecompilePipeline(RenderPipelines.GUI_TEXT_POLYGON_OFFSET);
        _device.PrecompilePipeline(RenderPipelines.GUI_TEXT_GRAYSCALE_SEE_THROUGH);
        _device.PrecompilePipeline(RenderPipelines.GUI_TEXT_GRAYSCALE_POLYGON_OFFSET);
    }

    //CreateBlurResources creates the two offscreen targets and the blur uniform buffer
    //The targets are sampleable and usable as color attachments; the uniform holds the blur direction and radius
    private void CreateBlurResources(int w, int h)
    {
        var usage = GpuTexture.UsageRenderAttachment | GpuTexture.UsageTextureBinding;
        _blurOffscreen = _device.CreateTexture(null, usage, GpuFormat.Rgba8Unorm, w, h, 1, 1);
        _blurTemp = _device.CreateTexture(null, usage, GpuFormat.Rgba8Unorm, w, h, 1, 1);
        _blurOffscreenView = _device.CreateTextureView(_blurOffscreen);
        _blurTempView = _device.CreateTextureView(_blurTemp);
        _blurSampler = _device.CreateSampler(AddressMode.ClampToEdge, AddressMode.ClampToEdge, FilterMode.Linear, FilterMode.Linear, 1, null);
        _blurUniformBuffer = _device.CreateBuffer(null, GpuBuffer.UsageUniform | GpuBuffer.UsageMapWrite, 16);
        _device.PrecompilePipeline(RenderPipelines.BLUR);
    }

    //DisposeBlurResources releases everything CreateBlurResources created
    private void DisposeBlurResources()
    {
        _blurOffscreenView?.Dispose();
        _blurTempView?.Dispose();
        _blurOffscreen?.Dispose();
        _blurTemp?.Dispose();
        _blurSampler?.Dispose();
        _blurUniformBuffer?.Dispose();
        _blurOffscreenView = null;
        _blurTempView = null;
        _blurOffscreen = null;
        _blurTemp = null;
        _blurSampler = null;
        _blurUniformBuffer = null;
    }

    //UpdateBlurUniform writes the blur direction and radius; direction (1,0) blurs horizontally and (0,1) vertically
    private void UpdateBlurUniform(float dirX, float dirY)
    {
        _blurUniformBuffer!.Upload(new float[] { dirX, dirY, BlurRadius, 0f });
    }

    //OnInitialized subscribes Silk input callbacks; the callbacks only enqueue, PollInput does the dispatching on the Tick thread
    protected override void OnInitialized()
    {
        _inputContext = _window.CreateInput();
        foreach (var mouse in _inputContext.Mice)
        {
            mouse.MouseDown += (_, b) => EnqueueMouse(b, down: true);
            mouse.MouseUp += (_, b) => EnqueueMouse(b, down: false);
            mouse.MouseMove += (_, p) => _inputQueue.Enqueue(new MouseMoveInput((int)p.X, (int)p.Y));
        }
        foreach (var keyboard in _inputContext.Keyboards)
        {
            keyboard.KeyDown += (_, k, _) => _inputQueue.Enqueue(new KeyInput((int)k, Down: true));
            keyboard.KeyUp += (_, k, _) => _inputQueue.Enqueue(new KeyInput((int)k, Down: false));
            keyboard.KeyChar += (_, c) => _inputQueue.Enqueue(new CharInput(c));
        }
    }

    //EnqueueMouse converts a Silk mouse button into a GuiMouseButton and enqueues a down/up event with the current cursor position
    //A button the GUI does not know maps to None and is dropped
    private void EnqueueMouse(MouseButton b, bool down)
    {
        var button = b switch
        {
            MouseButton.Left => GuiMouseButton.Left,
            MouseButton.Right => GuiMouseButton.Right,
            MouseButton.Middle => GuiMouseButton.Middle,
            _ => GuiMouseButton.None,
        };
        if (button == GuiMouseButton.None) return;
        var position = _inputContext?.Mice.FirstOrDefault()?.Position ?? default;
        var x = (int)position.X;
        var y = (int)position.Y;
        _inputQueue.Enqueue(down
            ? new MouseDownInput(button, x, y)
            : new MouseUpInput(button, x, y));
    }

    //PollInput drains the input queue on the Tick thread and dispatches to GuiWindow
    //Raw business events fire first so the Game layer sees keys and clicks the control tree does not consume
    public void PollInput()
    {
        while (_inputQueue.TryDequeue(out var inputEvent))
        {
            switch (inputEvent)
            {
                case MouseDownInput down:
                    RawMouseDown?.Invoke(down.Button, down.X, down.Y);
                    Window.ProcessMouseDown(down.Button, down.X, down.Y);
                    break;
                case MouseUpInput up:
                    RawMouseUp?.Invoke(up.Button, up.X, up.Y);
                    Window.ProcessMouseUp(up.Button, up.X, up.Y);
                    break;
                case MouseMoveInput move:
                    RawMouseMove?.Invoke(move.X, move.Y);
                    Window.ProcessMouseMove(move.X, move.Y);
                    break;
                case KeyInput key when key.Down:
                    //F11 is the vanilla default binding for key.fullscreen; the toggle only flips a flag and the frame
                    //loop applies it at the top of the next frame, where the surface is not acquired
                    if (key.Key == (int)Key.F11) ToggleFullScreen();
                    RawKeyDown?.Invoke(key.Key);
                    Window.ProcessKeyDown(key.Key);
                    break;
                case KeyInput key:
                    Window.ProcessKeyUp(key.Key);
                    break;
                case CharInput ch:
                    Window.ProcessKeyChar(ch.Char);
                    break;
            }
        }
    }

    //OnSwapchainRecreated rebuilds every extent dependent resource on window resize
    //_resourceManager/_renderContext are replaced under _resourceLock so SubmitFrame never reads half-swapped state
    //Old textureIds are invalidated; SwapchainRecreated at the end lets Screen re-Init and re-register its textures
    protected override void OnSwapchainRecreated()
    {
        var w = (int)_swapchainExtent.Width;
        var h = (int)_swapchainExtent.Height;
        GuiResourceManager? oldResourceManager;
        lock (_resourceLock)
        {
            oldResourceManager = _resourceManager;
            Window.UpdateSurfaceSize(w, h);
            _resourceManager = new GuiResourceManager(_device, w, h, _fontAtlas, _logger);
            //F7 rebuilds GlyphStitcher+Font; the new GuiResourceManager needs the atlas re-registered
            _glyphStitcher?.Dispose();
            _glyphStitcher = new GlyphStitcher(_device, _resourceManager);
            _font = CreateFontFromAssets();
            //P0 rebuilds GuiSpriteManager; old textureIds are invalidated and the cache is cleared so DrawSprite reloads
            _spriteManager = new GuiSpriteManager(_clientResources, _resourceManager, _guiAtlas);
            _renderContext = new GuiRenderContext(_renderState, w, h, Window.GuiScale,
                _font, _resourceManager.FontAtlas, _resourceManager.FontTexture,
                id => _resourceManager.ResolveTexture(id),
                _spriteManager);
            _device.ClearPipelineCache();
            PrecompileGuiPipelines();
            //Blur resource extent follows the swapchain; old resources are Disposed and rebuilt to match the new extent
            DisposeBlurResources();
            CreateBlurResources(w, h);
            //ItemAtlas is not rebuilt; AtlasTexture is only re-registered for a new textureId since the old _resourceManager is Disposed
            CreateItemAtlasResources();
            //World render resources rebuild the depth image to match the new extent; UBO/sampler are reused across resizes but the layout depends on the unchanged _device
            DisposeWorldResources();
            CreateWorldResources(w, h);
        }
        oldResourceManager?.Dispose();
        Interlocked.Exchange(ref _latestSnapshot, null);
        SwapchainRecreated?.Invoke();
    }

    //SubmitFrame the submission phase builds RenderStates and publishes a deep-copied snapshot to _latestSnapshot
    //The Render thread calls it from OnRecordCommandBuffer, before the GUI is recorded, holding _resourceLock so the
    //_renderContext/_renderState references stay stable against OnSwapchainRecreated
    //Inside the lock Window.Render calls _textureResolver, whose dictionary reads are mutually exclusive with RegisterTexture/OnSwapchainRecreated
    //P15 PreparePip moved to OnRecordCommandBuffer since vkQueueSubmit must be serial on the Render thread
    public void SubmitFrame()
    {
        if (_submitDiag++ < 3)
            Log.Info($"gui submit entered ctx={_renderContext is not null} rs={_renderState is not null}");
        GuiRenderContext? ctx;
        GuiRenderState? rs;
        var sw = Stopwatch.StartNew();
        try
        {
            lock (_resourceLock)
            {
                ctx = _renderContext;
                rs = _renderState;
                if (ctx is null || rs is null)
                {
                    if (_submitDiag++ % 60 == 0)
                        Log.Info($"gui submit skipped ctx={ctx is not null} rs={rs is not null}");
                    return;
                }
                rs.Reset();
                ctx.BeginFrame();
                if (_submitDiag < 5)
                    Log.Info("gui submit rendering window");
                Window.Render(ctx);
                if (_submitDiag < 5)
                    Log.Info("gui submit window rendered");
            }
            var snapshot = rs.Snapshot();
            Interlocked.Exchange(ref _latestSnapshot, snapshot);
            SubmissionCpuMs = sw.Elapsed.TotalMilliseconds;
        }
        catch (Exception ex)
        {
            //The tick loop stores this into _tickException and closes the window, which is silent until Stop,
            //so the cause is logged here as well
            if (_submitDiag++ % 60 == 0)
                Log.Info($"gui submit failed: {ex}");
            throw;
        }
    }

    //OnBeforeRun starts the Tick thread on the main thread before _window.Run, entering the 20tps loop
    //Run() calls this before entering the blocking _window.Run, when _resourceManager etc. are ready
    protected override void OnBeforeRun()
    {
        _tickThreadRunning = true;
        _tickThread = new Thread(TickThreadLoop)
        {
            IsBackground = true,
            Name = "NacreCraft.TickThread"
        };
        _tickThread.Start();
    }

    //OnAfterRun Joins the Tick thread on the main thread after _window.Run exits
    //_window.Close already made _window.Run exit; this method sets _tickThreadRunning=false so the Tick loop exits naturally
    //Join times out after 2s to prevent a hang; on a Tick exception RequestClose was already called so the main thread always reaches here
    //After Join it checks _tickException and rethrows if non-null so MinecraftClient.Run's finally sees it
    protected override void OnAfterRun()
    {
        _tickThreadRunning = false;
        var t = _tickThread;
        if (t is not null && !t.Join(TickJoinTimeoutMs))
        {
            throw new TimeoutException($"Tick thread {TickJoinTimeoutMs}ms did not exit, suspected deadlock");
        }
        if (_tickException is not null)
        {
            var ex = _tickException;
            _tickException = null;
            throw new InvalidOperationException("Tick thread exception", ex);
        }
    }

    //TickThreadLoop the game logic loop
    //Stopwatch measures each cycle; if under the target interval Thread.Sleep makes up the difference
    //Each cycle only runs FrameTick so world logic keeps its own rate; the GUI scene is built on the render thread
    //Exceptions are captured into _tickException and RequestClose lets the main thread's OnAfterRun rethrow
    private void TickThreadLoop()
    {
        try
        {
            //Raise the timer resolution for the life of this thread, otherwise the frame waits below get rounded to ticks
            if (OperatingSystem.IsWindows()) TimeBeginPeriod(1);
            var watch = Stopwatch.StartNew();
            var lastElapsed = 0.0;
            var tickCount = 0;
            var lastTpsTime = 0.0;
            //Debug totals, split so the slow half of the cycle is visible
            var tickTotalMs = 0.0;
            while (_tickThreadRunning)
            {
                var elapsed = watch.Elapsed.TotalSeconds;
                var delta = elapsed - lastElapsed;
                lastElapsed = elapsed;
                var tickStart = watch.Elapsed.TotalMilliseconds;
                FrameTick?.Invoke(delta);
                tickTotalMs += watch.Elapsed.TotalMilliseconds - tickStart;
                tickCount++;
                if (elapsed - lastTpsTime >= 1.0)
                {
                    TickRate = tickCount;
                    //Debug: this is the rate the picture actually changes at, independent of the renderer's frame rate
                    if (Log.DebugEnabled)
                        Log.Debug("tick rate " + tickCount + " tps tick avg " + (tickTotalMs / tickCount).ToString("F1") + "ms");
                    tickCount = 0;
                    tickTotalMs = 0;
                    lastTpsTime = elapsed;
                }
                //Wait out the rest of the frame: sleep in 1ms steps while there is room, then spin through the last
                //couple of milliseconds. With the 1ms timer resolution this lands within a millisecond of the target
                var deadline = elapsed + TickTargetInterval;
                while (watch.Elapsed.TotalSeconds < deadline)
                {
                    var remainingMs = (deadline - watch.Elapsed.TotalSeconds) * 1000.0;
                    if (remainingMs > 2.0) Thread.Sleep(1);
                    else Thread.SpinWait(64);
                }
            }
        }
        catch (Exception ex)
        {
            //Stored for Stop to rethrow, but until then the window keeps rendering without a snapshot,
            //so the cause is logged right away
            Log.Info($"gui tick thread failed: {ex}");
            _tickException = ex;
            RequestClose();
        }
    }

    //OnRecordCommandBuffer builds the GUI for this frame and then records it
    //GuiTick runs first on the Render thread: input, layout, widget animation; SubmitFrame then turns the control tree
    //into a snapshot, which is consumed right below
    //P15 PreparePip on the Render thread holds the lock and calls PIP renderer.Prepare for offscreen rendering+blit
    //vkQueueSubmit must be serial on the Render thread; nothing on the Tick thread submits to the queue
    //On the first frame with no snapshot it only clears without binding a pipeline or drawing
    //Stage 7 holds _resourceLock to protect the _resourceManager field reference and inner dictionaries, mutually exclusive with RegisterTexture
    //_guiPipeline/cmd/clearColor do not depend on _resourceManager and are prepared outside the lock
    protected override void OnRecordCommandBuffer(CommandEncoder encoder, GpuTextureView sceneView)
    {
        //Widgets update here rather than on the tick thread: vanilla computes hover inside renderWidget, so the picture
        //follows the frame rate instead of being capped by the game tick
        var guiNow = Stopwatch.GetTimestamp();
        var guiDelta = _lastGuiTimestamp == 0 ? 0.0 : (guiNow - _lastGuiTimestamp) / (double)Stopwatch.Frequency;
        _lastGuiTimestamp = guiNow;
        GuiTick?.Invoke(guiDelta);
        SubmitFrame();
        //Queued glyph uploads are drained after the scene is built, so glyphs baked this frame are on the atlas before
        //the draw that samples them; this is the last point before recording that can submit on its own
        GpuTexture.FlushPendingRegionUploads(_device);
        var snapshot = _latestSnapshot;
        var bg = Window.BackgroundColor;
        var clearColor = new Vector4(bg.R, bg.G, bg.B, bg.A);
        //Debug diagnostic: frames are written next to the executable, so the rendered GUI can be inspected without a
        //screen capture tool. The early frames catch the loading overlay, which is gone within a couple of seconds
        if (Log.DebugEnabled && (++_dumpFrame <= 3 || _dumpFrame == 5 || _dumpFrame == 10 || _dumpFrame == 20 || _dumpFrame == 60 || _dumpFrame == 200 || _dumpFrame == 600 || _dumpFrame == 1200 || _dumpFrame == 2000))
        {
            var scenePixels = _sceneColor.Readback();
            var sceneDumpPath = Path.Combine(AppContext.BaseDirectory, $"scene-dump-{_dumpFrame}.bmp");
            WriteBmp(sceneDumpPath, scenePixels, _sceneColor.Width, _sceneColor.Height);
            Log.Debug($"gui scene sample {_sceneColor.Width}x{_sceneColor.Height} topLeft=({scenePixels[0]},{scenePixels[1]},{scenePixels[2]},{scenePixels[3]}) dumped to {sceneDumpPath}");
        }
        if (snapshot is null)
        {
            if (_diagFrame++ % 60 == 0)
                Log.Debug("gui frame recorded without a snapshot yet, only the clear pass runs");
            //First frame with no snapshot published by Tick: only clear the scene target without Draw
            var clearDescriptor = RenderPassDescriptor.Create(() => "gui-clear")
                .WithColorAttachment(sceneView, clearColor)
                .WithRenderArea(SceneRenderArea());
            using var clearPass = encoder.CreateRenderPass(clearDescriptor);
            return;
        }
        var sw = Stopwatch.StartNew();
        //passKind records which recording branch ran, so a blank frame can be traced to the pass it took
        string passKind;
        //P15 PreparePip does the PIP offscreen rendering+blit before the main recording
        //PIP renderer.Prepare creates an independent encoder and only after its Submit completes does the main recording start
        try
        {
        lock (_resourceLock)
        {
            _guiRenderer.PreparePip(snapshot, Window.GuiScale);
            //W8.5 world render Prepare+Upload moved before recording to avoid RunOneTimeCommand on device-local buffers
            if (WorldRenderEnabled && _depthImage is not null)
            {
                _worldViewProjBuffer!.Upload<Matrix4x4>(new[] { _levelRenderer!.ViewProj });
                _levelRenderer.Prepare();
                _levelRenderer.Upload(_device);
            }
        }
        lock (_resourceLock)
        {
            _resourceManager.UpdateProjection();
            _guiRenderer.Prepare(snapshot);
            _guiRenderer.Upload(_device);
            //When world rendering is enabled it uses a world+GUI composite pass; blur post-processing is not yet in the same frame as world rendering
            if (WorldRenderEnabled && _depthImage is not null)
            {
                passKind = "world+gui";
                RenderWorldAndGuiPass(encoder, sceneView, clearColor);
            }
            else if (snapshot.HasBlurSplit && _blurOffscreenView is not null && _blurTempView is not null)
            {
                passKind = "blur";
                RenderBlurPasses(encoder, sceneView, clearColor);
            }
            else
            {
                passKind = "single";
                RenderSinglePass(encoder, sceneView, clearColor);
            }
        }
        RenderCpuMs = sw.Elapsed.TotalMilliseconds;
        if (_diagFrame++ % 60 == 0)
            Log.Debug($"gui frame recorded pass={passKind} meshes={_guiRenderer.MeshCount} vertices={_guiRenderer.VertexCount} drawCalls={_guiRenderer.DrawCallCount} worldEnabled={WorldRenderEnabled} worldVertices={WorldVertexCount} blurSplit={snapshot.HasBlurSplit} clear=({(int)(clearColor.X * 255)},{(int)(clearColor.Y * 255)},{(int)(clearColor.Z * 255)},{(int)(clearColor.W * 255)})");
        }
        catch (Exception ex)
        {
            //Without this the render thread swallows the failure and the frame keeps showing only the clear color
            Log.Debug($"gui frame recording failed: {ex}");
        }
    }

    //WriteBmp saves an RGBA8 pixel buffer as a 32 bit BMP, the format Windows viewers open directly
    //Rows are stored bottom up and as BGRA, so the rows are walked backwards and the red and blue channels swapped
    private static void WriteBmp(string path, byte[] rgba, int width, int height)
    {
        var stride = width * 4;
        var imageSize = stride * height;
        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);
        writer.Write((byte)'B');
        writer.Write((byte)'M');
        writer.Write(14 + 40 + imageSize);
        writer.Write(0);
        writer.Write(14 + 40);
        writer.Write(40);
        writer.Write(width);
        writer.Write(height);
        writer.Write((short)1);
        writer.Write((short)32);
        writer.Write(0);
        writer.Write(imageSize);
        writer.Write(2835);
        writer.Write(2835);
        writer.Write(0);
        writer.Write(0);
        for (var y = height - 1; y >= 0; y--)
        {
            var rowStart = y * stride;
            for (var x = 0; x < width; x++)
            {
                var pixel = rowStart + x * 4;
                writer.Write(rgba[pixel + 2]);
                writer.Write(rgba[pixel + 1]);
                writer.Write(rgba[pixel]);
                writer.Write(rgba[pixel + 3]);
            }
        }
    }

    //SceneRenderArea the full-extent render area
    private NacreCraft.Client.Blaze3d.Systems.RenderPass.RenderArea SceneRenderArea()
        => new(0, 0, (int)_swapchainExtent.Width, (int)_swapchainExtent.Height);

    //BindGuiGlobals writes the GUI global uniforms for the pipeline layout just bound
    private void BindGuiGlobals(NacreCraft.Client.Blaze3d.Systems.RenderPass pass)
    {
        pass.SetUniform("Globals", _resourceManager.GlobalsBuffer);
        pass.SetUniform("Matrices", _resourceManager.ProjectionBuffer);
    }

    //RenderSinglePass single render pass without blur splitting, rendering everything to the scene target
    private void RenderSinglePass(CommandEncoder encoder, GpuTextureView sceneView, Vector4 clearColor)
    {
        var descriptor = RenderPassDescriptor.Create(() => "gui")
            .WithColorAttachment(sceneView, clearColor)
            .WithRenderArea(SceneRenderArea());
        using var pass = encoder.CreateRenderPass(descriptor);
        _guiRenderer.Draw(pass, BindGuiGlobals, t => _resourceManager.ResolveTextureBinding(t));
    }

    //RenderWorldAndGuiPass world rendering+GUI composite in two passes
    //The world pass clears color and depth, drawing terrain in Solid→Cutout→Translucent order
    //The GUI pass LoadOp=Load preserves the world color with no depth, overlaying GUI widgets
    //Prepare/Upload already completed before BeginRecording in OnRecordCommandBuffer, avoiding RunOneTimeCommand polluting the main cmd
    private void RenderWorldAndGuiPass(CommandEncoder encoder, GpuTextureView sceneView, Vector4 clearColor)
    {
        var area = SceneRenderArea();
        //The world pass clears color+depth and binds the view-projection matrix by name plus the atlas/lightmap samplers
        var worldDescriptor = RenderPassDescriptor.Create(() => "world")
            .WithColorAttachment(sceneView, clearColor)
            .WithDepthAttachment(_depthView!, 1.0)
            .WithRenderArea(area);
        using (var worldPass = encoder.CreateRenderPass(worldDescriptor))
        {
            _levelRenderer!.Draw(worldPass, BindWorldGlobals);
        }

        //The GUI pass uses LoadOp=Load to preserve the world render result with no depth attachment
        var guiDescriptor = RenderPassDescriptor.Create(() => "gui")
            .WithColorAttachment(sceneView)
            .WithRenderArea(area);
        using var guiPass = encoder.CreateRenderPass(guiDescriptor);
        _guiRenderer.Draw(guiPass, BindGuiGlobals, t => _resourceManager.ResolveTextureBinding(t));
    }

    //BindWorldGlobals writes the terrain uniforms for the pipeline layout just bound
    private void BindWorldGlobals(NacreCraft.Client.Blaze3d.Systems.RenderPass pass)
    {
        pass.SetUniform("Matrices", _worldViewProjBuffer!);
        pass.BindTexture("Sampler0", _worldBlockAtlasView!, _worldBlockAtlasSampler!);
        pass.BindTexture("Sampler1", _worldLightmapView!, _worldLightmapSampler!);
    }

    //RenderBlurPasses segmented rendering BeforeBlur→horizontal blur→vertical blur→AfterBlur
    //BeforeBlur renders to offscreen, horizontally blurs offscreen→temp and vertically blurs temp→scene
    //The AfterBlur segment uses LoadOp=Load to preserve the blurred background with GUI widgets overlaid
    private void RenderBlurPasses(CommandEncoder encoder, GpuTextureView sceneView, Vector4 clearColor)
    {
        var offscreen = (VulkanImage)_blurOffscreen!;
        var temp = (VulkanImage)_blurTemp!;
        var vkEncoder = (VulkanCommandEncoder)encoder.Backend;
        var firstAfterBlur = _guiRenderer.FirstMeshIndexAfterBlur;
        var area = SceneRenderArea();

        //The BeforeBlur segment renders to offscreen after transitioning it to a color attachment layout
        offscreen.TransitionLayout(vkEncoder.Handle, ImageLayout.ColorAttachmentOptimal);
        using (var beforePass = encoder.CreateRenderPass(RenderPassDescriptor.Create(() => "blur-before")
            .WithColorAttachment(_blurOffscreenView!, clearColor)
            .WithRenderArea(area)))
        {
            _guiRenderer.DrawRange(beforePass, BindGuiGlobals, t => _resourceManager.ResolveTextureBinding(t), 0, firstAfterBlur);
        }

        //Horizontal blur offscreen→temp, offscreen to ShaderReadOnly, temp to ColorAttachmentOptimal
        offscreen.TransitionLayout(vkEncoder.Handle, ImageLayout.ShaderReadOnlyOptimal);
        temp.TransitionLayout(vkEncoder.Handle, ImageLayout.ColorAttachmentOptimal);
        UpdateBlurUniform(1f, 0f);
        using (var hBlurPass = encoder.CreateRenderPass(RenderPassDescriptor.Create(() => "blur-h")
            .WithColorAttachment(_blurTempView!, clearColor)
            .WithRenderArea(area)))
        {
            hBlurPass.SetPipeline(RenderPipelines.BLUR);
            hBlurPass.SetUniform("BlurConfig", _blurUniformBuffer!);
            hBlurPass.BindTexture("InSampler", _blurOffscreenView!, _blurSampler!);
            hBlurPass.DisableScissor();
            hBlurPass.Draw(6, 1, 0, 0);
        }

        //Vertical blur temp→scene, temp to ShaderReadOnlyOptimal, scene overwritten with LoadOp=Clear
        temp.TransitionLayout(vkEncoder.Handle, ImageLayout.ShaderReadOnlyOptimal);
        UpdateBlurUniform(0f, 1f);
        using (var vBlurPass = encoder.CreateRenderPass(RenderPassDescriptor.Create(() => "blur-v")
            .WithColorAttachment(sceneView, clearColor)
            .WithRenderArea(area)))
        {
            vBlurPass.SetPipeline(RenderPipelines.BLUR);
            vBlurPass.SetUniform("BlurConfig", _blurUniformBuffer!);
            vBlurPass.BindTexture("InSampler", _blurTempView!, _blurSampler!);
            vBlurPass.DisableScissor();
            vBlurPass.Draw(6, 1, 0, 0);
        }

        //The AfterBlur segment uses LoadOp=Load to keep the blurred result and overlays the remaining widgets
        using var afterPass = encoder.CreateRenderPass(RenderPassDescriptor.Create(() => "blur-after")
            .WithColorAttachment(sceneView)
            .WithRenderArea(area));
        _guiRenderer.DrawRange(afterPass, BindGuiGlobals, t => _resourceManager.ResolveTextureBinding(t), firstAfterBlur, _guiRenderer.Meshes.Count);
    }

    //OnCleanupPipelineResources releases the GUI/world resource chain
    //Injected atlas/lightmap are released here because the Game layer handed ownership over
    protected override void OnCleanupPipelineResources()
    {
        _resourceManager.Dispose();
        _guiRenderer.Dispose();
        DisposeBlurResources();
        _itemAtlas?.Dispose();
        _itemPipRenderer?.Dispose();
        _itemAtlas = null;
        _itemPipRenderer = null;
        _itemAtlasTextureId = 0;
        _glyphStitcher?.Dispose();
        _glyphStitcher = null;
        //The GUI atlas holds a device texture, so it goes with the device rather than with the swapchain
        _guiAtlas?.Dispose();
        _guiAtlas = null;
        _spriteManager = null;
        _font = null;
        if (_inputContext is not null)
        {
            _inputContext.Dispose();
            _inputContext = null;
        }
        _injectedBlockAtlas?.Dispose();
        _injectedBlockAtlas = null;
        _injectedLightmap?.Dispose();
        _injectedLightmap = null;
    }

    public override void Dispose()
    {
        if (_disposed) return;
        base.Dispose();
        Window.Dispose();
        _disposed = true;
    }
}

//InputEvent queued by the Silk input callbacks and drained by PollInput on the Tick thread
internal abstract record InputEvent;
internal sealed record MouseDownInput(GuiMouseButton Button, int X, int Y) : InputEvent;
internal sealed record MouseUpInput(GuiMouseButton Button, int X, int Y) : InputEvent;
internal sealed record MouseMoveInput(int X, int Y) : InputEvent;
internal sealed record KeyInput(int Key, bool Down) : InputEvent;
internal sealed record CharInput(char Char) : InputEvent;
