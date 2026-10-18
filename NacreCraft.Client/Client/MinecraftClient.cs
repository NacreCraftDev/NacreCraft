using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using NacreCraft.Client.Level;
using NacreCraft.Client.Language;
using NacreCraft.Client.Render;
using NacreCraft.Client.Render.Atlas;
using NacreCraft.Client.Render.Entity;
using NacreCraft.Client.Render.Model;
using NacreCraft.Client.Render.World;
using NacreCraft.Client.Gui;
using NacreCraft.Client.Gui.Screens;
using NacreCraft.Game;
using NacreCraft.Game.Network;
using NacreCraft.Game.World.Entity;
using NacreCraft.Client.Blaze3d.Systems;
using NacreCraft.Client.Blaze3d.Buffers;
using NacreCraft.Client.Blaze3d.Textures;
using NacreCraft.Client.Blaze3d.Shaders;
using NacreCraft.Client.Blaze3d.Pipeline;
using NacreCraft.Client.Blaze3d.Vertex;
using NacreCraft.Client.Blaze3d.Vulkan;
using NacreCraft.Client.Blaze3d.Platform;
using NacreCraft.Client.Blaze3d.Font;
using NacreCraft.Logging;
using NacreCraft.Network;
using NacreCraft.Resources;
using NacreCraft.Client.Blaze3d.Resource;
using NacreCraft.Client.Blaze3d.Audio;
using NacreCraft.Client.Blaze3d.Framegraph;
using NacreCraft.Client.Blaze3d.Preprocessor;
using NacreCraft.Client.Blaze3d.Util;
using NacreCraft.Client.Render.Texture;
using NacreCraft.Client.Render.Texture.Atlas;
using NacreCraft.Client.Render.Item;
using NacreCraft.Client.Render.Entity.State;
using NacreCraft.Client.Render.State.Gui;
using NacreCraft.Client.Gui.Render;
using NacreCraft.Client.Gui.Render.State;
using NacreCraft.Client.Gui.Render.Pip;
using NacreCraft.Client.Gui.Navigation;
using NacreCraft.Client.Gui.Layouts;
using NacreCraft.Client.Gui.Font;
using NacreCraft.Client.Gui.Font.Providers;
using NacreCraft.Client.Gui.Font.Glyphs;
using NacreCraft.Client.Model;
using NacreCraft.Client.Model.Geom;
using NacreCraft.Client.Resources.Metadata.Gui;

namespace NacreCraft.Client;

//MinecraftClient client main loop, maps to vanilla net.minecraft.client.Minecraft
//Holds GameConfig and runtime state, providing the Run and Stop main loop skeleton
//Phase 11.35 added the empty loop shell, 11.46 added frame rate control, 11.49 added dual modes + the Tick quartet
//Dual mode: with gpuApp it runs window-driven with Tick on FrameUpdate; without gpuApp it runs while+sleep for tests
//S3 holds the ClientLevel/LevelRenderer world render chain; ConnectServer establishes a network connection, receives chunks, and displays terrain
public sealed class MinecraftClient : IDisposable
{
    //TargetFps target frames per second, aligned to 60 FPS
    public const int TargetFps = 60;
    //TargetFrameMillis target per-frame duration, about 16.67ms
    public const int TargetFrameMillis = 1000 / TargetFps;

    private volatile bool _running;
    private long _frameCount;
    private readonly CancellationTokenSource _shutdownCts = new();
    //SleepBudgetMillis per-frame sleep budget; tests set 0 to speed up, production uses the 16ms default
    private int _sleepBudgetMillis = TargetFrameMillis;
    //GpuApp nullable; null runs Headless mode for server integration and GPU-less tests
    private readonly VulkanGuiApp? _gpuApp;
    //Connection nullable; null skips network packet processing for singleplayer or test scenarios
    private Connection? _connection;
    //ScreenManager nullable; null skips screen logic for Headless tests
    private readonly ScreenManager? _screens;
    //ServerResources nullable; null skips data-driven queries such as Tags for Headless tests
    private readonly ReloadableServerResources? _rsr;
    //_pendingTasks work queued from other threads, drained on the tick thread; maps to vanilla BlockableEventLoop.pendingRunnables
    private readonly ConcurrentQueue<System.Action> _pendingTasks = new();
    //_guiTasks work queued for the render thread; the control tree belongs to the render thread, so anything that
    //switches screens from game logic goes through here instead of touching the GUI off-thread
    private readonly ConcurrentQueue<System.Action> _guiTasks = new();
    //ResourceManager resource manager for block model loading and texture collection
    private readonly ResourceManager? _resourceManager;
    //PackRepository the repository the resource manager was built from; a reload re-discovers it and rebuilds the manager
    private readonly PackRepository? _packRepository;
    //Language client language table; nullable, null means no language switching, for Headless tests
    private readonly ClientLanguage? _language;
    //ClientLevel client world holding network chunks, filled by the listener after ConnectServer
    private readonly ClientLevel _level = new();
    //Camera world render camera, updated each Tick from the local player state
    private readonly Camera _camera = new();
    //World render chain created once the swapchain is ready on the first frame and injected into gpuApp
    private SectionRenderDispatcher? _worldDispatcher;
    private LevelRenderer? _worldRenderer;
    private bool _worldRendererInitialized;
    //Lightmap light texture refreshed each Tick from Level.SkyBrightness; internal dirtiness check avoids re-upload
    private LightTexture? _lightTexture;

    //GameConfig client config, the options.txt load result
    public GameConfig Config { get; }

    //Player local player entity; HUD and game logic read Health/Food/Pos etc.
    public World.Entity.Player Player { get; } = new();

    //GpuApp held Vulkan GUI app, non-null in window-driven mode
    public VulkanGuiApp? GpuApp => _gpuApp;

    //Connection held network connection; when non-null, Tick calls its Tick to process inbound packets
    public Connection? Connection => _connection;

    //Screens screen manager; when non-null it drives the screen lifecycle
    public ScreenManager? Screens => _screens;

    //ServerResources server reloadable resource set; when non-null it serves client data-driven queries such as Tags
    public ReloadableServerResources? ServerResources => _rsr;

    //Resources resource manager; when non-null, the language screen calls Reload after switching language to rerun the reload chain
    public ResourceManager? Resources => _resourceManager;

    //Language client language table; when non-null, the language screen reads/writes the current code and available list
    public ClientLanguage? Language => _language;

    //Level client world chunk data source, for diagnostics
    public ClientLevel Level => _level;

    //SetScreen switches screens, delegated to ScreenManager; null means close the current screen
    public void SetScreen(Screen? screen) => _screens?.SetScreen(screen);

    //PushScreen pushes the current screen onto the stack for submenus; Esc pops back
    public void PushScreen(Screen screen) => _screens?.PushScreen(screen);

    //PopScreen pops back to the previous level; if empty, closes the current screen
    public void PopScreen() => _screens?.PopScreen();

    public MinecraftClient(GameConfig config, VulkanGuiApp? gpuApp = null, Connection? connection = null, ReloadableServerResources? rsr = null, ResourceManager? resourceManager = null, ClientLanguage? language = null, PackRepository? packRepository = null)
    {
        Config = config;
        _gpuApp = gpuApp;
        _connection = connection;
        _rsr = rsr;
        _resourceManager = resourceManager;
        _language = language;
        _packRepository = packRepository;
        //With a GpuApp, create a ScreenManager to own the window control tree
        _screens = gpuApp is not null ? new ScreenManager(this, gpuApp.Window) : null;
        if (gpuApp is not null && _screens is not null)
        {
            //Subscribe to the swapchain-recreated event so on window resize ScreenManager re-layouts the current Screen
            gpuApp.SwapchainRecreated += () => _screens.Resized();
            //Raw key and mouse events must reach the screen manager; a missed subscription would prevent Esc/F3/number keys/Q and mouse clicks from reaching a Screen
            gpuApp.RawKeyDown += _screens.HandleRawKeyDown;
            gpuApp.RawMouseDown += _screens.HandleRawMouseDown;
            gpuApp.RawMouseUp += _screens.HandleRawMouseUp;
            gpuApp.RawMouseMove += _screens.HandleRawMouseMove;
        }
        //The world render chain is built by the startup loading stage rather than a swapchain event, so the loading
        //overlay has real work to show; EnsureWorldRenderer stays idempotent for callers that run it more than once
    }

    //EnsureWorldRenderer builds the block atlas and LevelRenderer into gpuApp the first time the swapchain is ready
    //If atlas building fails, the placeholder texture remains and world rendering still works; mesh textures use the placeholder color
    private void EnsureWorldRenderer()
    {
        if (_worldRendererInitialized) return;
        _worldRendererInitialized = true;
        if (_gpuApp is null || _resourceManager is null) return;
        try
        {
            var collector = new BlockTextureCollector(_resourceManager);
            var sprites = collector.Collect();
            BlockTextureAtlas? atlas = null;
            if (sprites.Count > 0)
            {
                atlas = new BlockTextureAtlas(_gpuApp.Device);
                if (!atlas.Build(sprites))
                {
                    atlas.Dispose();
                    atlas = null;
                    Log.Warning($"Block texture atlas has {sprites.Count} sprites exceeding the limit, build failed, falling back to placeholder");
                }
            }
            else
            {
                Log.Warning("Block texture collection is empty, skipping real atlas injection and using placeholder texture");
            }
            if (atlas is not null)
            {
                _gpuApp.SetBlockAtlas(atlas);
                _lightTexture = new LightTexture(_gpuApp.Device);
                _gpuApp.SetLightmap(_lightTexture);
                Log.Info($"Block texture atlas built sprite={sprites.Count}");
            }
            var loader = new BlockModelLoader(_resourceManager);
            var baker = new BlockModelBaker(atlas ?? PlaceholderAtlas);
            var mapper = new BlockStateModelMapper(_resourceManager, loader, baker);
            //Inject the light sampler; mesh vertex light attributes fetch chunk light from ClientLevel
            var builder = new ChunkMeshBuilder(mapper, new ChunkLightSampler(_level));
            var pool = new GpuBufferPool((size, usage) => _gpuApp.Device.CreateHostVisibleBuffer(size, usage));
            _worldDispatcher = new SectionRenderDispatcher(_level, builder, pool, workerCount: 2);
            _worldDispatcher.Start();
            _worldRenderer = new LevelRenderer(_level, _camera, _worldDispatcher, pool);
            //The entity render dispatcher is created along with world rendering
            //Dropped items hold their own model mapper instance; the background mesh-building thread already uses that mapper's cache dictionary, and sharing it would cause concurrent reads/writes
            var entityDispatcher = new EntityRenderDispatcher(pool);
            entityDispatcher.Register(EntityTypes.ITEM.Id.ToString(),
                new ItemEntityRenderer(new BlockStateModelMapper(_resourceManager, loader, baker)));
            _worldRenderer.EntityDispatcher = entityDispatcher;
            //The moving block pass holds its own model mapper and mesh builder; the background mesh-building thread already uses that mapper's cache dictionary
            _worldRenderer.MovingBlocks = new MovingBlockRenderer(_level,
                new ChunkMeshBuilder(new BlockStateModelMapper(_resourceManager, loader, baker),
                    new ChunkLightSampler(_level)), pool);
            _gpuApp.SetLevelRenderer(_worldRenderer);
            Log.Info("World render pipeline created");
        }
        catch (Exception ex)
        {
            Log.Warning($"World render pipeline creation failed {ex.Message}");
        }
    }

    //PlaceholderAtlas placeholder used when the atlas is absent, avoiding a null reference in BlockModelBaker
    //sprite UV covers the whole [0,1] image; the mesh samples the placeholder texture as a solid color
    private static ITextureAtlas PlaceholderAtlas => new PlaceholderTextureAtlas();

    //PlaceholderTextureAtlas placeholder atlas mapping all texture names to the same 16x16 sprite
    private sealed class PlaceholderTextureAtlas : ITextureAtlas
    {
        //16x16 sprite covers the full UV of a 16x16 atlas
        private static readonly TextureAtlasSprite s_sprite =
            new("placeholder", 0, 0, 16, 16, 16, 16, null);

        public TextureAtlasSprite? GetSprite(string name) => s_sprite;
    }

    //ConnectServer: a background thread connects to the server; on success it holds the Connection and goes Login->Configuration->Play
    //On entering the world it switches to GameScreen; network chunks load into ClientLevel via ClientGamePacketListenerImpl
    public void ConnectServer(string host, int port)
    {
        Log.Info($"Connecting to server {host}:{port}");
        ClientConnector.Connect(host, port, _level, Player,
            onJoinWorld: () =>
            {
                SubmitGui(() => _screens?.SetScreen(new GameScreen()));
                Log.Info("Entered game, switching to GameScreen");
            },
            onConnected: conn =>
            {
                _connection = conn;
                if (conn.Listener is ClientGamePacketListenerImpl listener)
                {
                    //The container screen is switched in a network callback; the network layer does not know the GUI layer, and the row count is decided by the menu type
                    //Only chest types have a ready screen; crafting-table-style screens are not done yet, so unrecognized types do not open a screen to avoid wrongly opening a chest
                    listener.OnOpenScreen = packet =>
                    {
                        if (ChestScreen.IsChestKind(packet.Kind))
                            SubmitGui(() => _screens?.SetScreen(new ChestScreen(packet.Title, ChestScreen.RowsFor(packet.Kind))));
                    };
                    listener.OnContainerClose = () =>
                    {
                        SubmitGui(() =>
                        {
                            if (_screens?.Current is ChestScreen) _screens.SetScreen(new GameScreen());
                        });
                    };
                }
            },
            onFailed: reason => Log.Error($"Failed to connect to server {reason}"));
    }

    //Running whether currently in the main loop
    public bool Running => _running;

    //FrameCount accumulated frame count, for diagnostics
    public long FrameCount => _frameCount;

    //SleepBudgetMillis per-frame sleep budget; tests set 0 to speed up, production uses the 16ms default
    //Only effective in Headless mode; window-driven mode controls frame rate via vsync
    public int SleepBudgetMillis
    {
        get => _sleepBudgetMillis;
        set => _sleepBudgetMillis = Math.Max(0, value);
    }

    //CreateStartupOverlay builds the loading overlay covering the startup work
    //The world render chain decodes every block texture and takes a noticeable while, so it runs on a background
    //thread and the overlay polls it; the title screen is already up underneath, so the fade out lands on it
    private LoadingOverlay? CreateStartupOverlay()
    {
        if (_screens is null || _gpuApp is null) return null;
        var worldBuild = Task.Run(EnsureWorldRenderer);
        var stages = new List<(string Label, Func<float>)>
        {
            //The background build reports no intermediate progress, so the bar holds until it lands
            ("world", () => worldBuild.IsCompleted ? 1f : 0f)
        };
        //Startup does not fade in, matching vanilla where only a manual reload passes fadeIn true
        //A fade in would start at a low alpha and let the title screen show through before the cover lands
        return new LoadingOverlay(_gpuApp.Window, RegisterLogoTexture, stages, fadeIn: false);
    }

    //RegisterLogoTexture loads the Mojang Studios logo the loading overlay draws
    private int RegisterLogoTexture()
        => _gpuApp?.RegisterTexture(Identifier.WithDefaultNamespace("textures/gui/title/mojangstudios.png")) ?? 0;

    //ReloadResourcePacks re-discovers the pack sources, rebuilds the resource manager and runs the listener chain
    //maps to vanilla Minecraft.reloadResourcePacks: the work runs inside a loading overlay instead of blocking the caller
    public void ReloadResourcePacks()
    {
        if (_packRepository is null || _rsr is null || _resourceManager is null) return;
        //Without a window there is no overlay to cover the work, so the chain runs inline
        if (_screens is null || _gpuApp is null)
        {
            ApplyPackSelection();
            _rsr.Reload();
            return;
        }
        //A second request while an overlay is already up is dropped, mirroring vanilla pendingReload
        if (_screens.Overlay is LoadingOverlay) return;
        ReloadInstance? reload = null;
        var stages = new List<(string Label, Func<float>)>
        {
            ("packs", () =>
            {
                if (reload is null)
                {
                    ApplyPackSelection();
                    reload = _rsr.BeginReload();
                }
                reload.Pump();
                if (!reload.IsDone) return reload.ActualProgress;
                reload.CheckExceptions();
                _rsr.FinishReload();
                return 1f;
            })
        };
        _screens.SetOverlay(new LoadingOverlay(_gpuApp.Window, () => 0, stages, OnResourceReloadFinished));
    }

    //ApplyPackSelection re-discovers the sources, applies the saved selection and rebuilds the resource manager
    //The selection is written back without the fixed-position entries, matching vanilla Options.updateResourcePacks
    private void ApplyPackSelection()
    {
        //The pack folder was scanned when the repository was built, so this only re-applies the selection
        //An explicit refresh is what picks up a pack dropped into resourcepacks/ while the game is running
        _packRepository!.SetSelected(Config.ResourcePacks);
        _resourceManager!.SetPacks(_packRepository.Selected);
        Config.ResourcePacks = _packRepository.Selected.Where(p => !p.IsFixedPosition).Select(p => p.Id).ToList();
        Config.Save(AppPaths.OptionsPath);
        Log.Info($"Resource packs reloaded: available={_packRepository.Available.Count} selected={_packRepository.Selected.Count}");
    }

    //OnResourceReloadFinished refreshes whatever mirrors the pack contents now that the chain is done
    private void OnResourceReloadFinished()
    {
        //GUI textures and fonts come from packs too, so their caches go first
        _gpuApp?.ReloadResources();
        _screens?.RebuildCurrent();
    }

    //Submit queues work to run on the tick thread, maps to vanilla Minecraft.submit(Runnable)
    //Any thread may call it, which is what lets external code touch client state without racing the render thread
    public void Submit(System.Action task) => _pendingTasks.Enqueue(task);

    //SubmitGui queues work to run on the render thread before the next frame is built
    //Used by game logic that opens or closes screens, since the control tree is owned by the render thread
    public void SubmitGui(System.Action task) => _guiTasks.Enqueue(task);

    //RunQueuedTasks drains the queued work, maps to vanilla BlockableEventLoop.runAllTasks
    //A failing task is logged and dropped so one bad task cannot stall the loop
    private void RunQueuedTasks()
    {
        while (_pendingTasks.TryDequeue(out var task))
        {
            try
            {
                task();
            }
            catch (Exception ex)
            {
                Log.Warning($"Queued client task failed: {ex}");
            }
        }
    }

    //Run main loop entry; blocks the calling thread until Stop is called
    //With gpuApp, Render goes through the window loop and Tick is dispatched by FrameTick from gpuApp's internal Tick thread
    //Without gpuApp it runs while+sleep at 60 FPS for Headless tests
    public void Run()
    {
        if (_running) return;
        _running = true;
        if (_gpuApp is not null)
        {
            Log.Info($"MinecraftClient window-driven mode started render distance {Config.RenderDistance} FOV {Config.Fov}");
            //The title screen goes up first and the loading overlay covers it, which is the order vanilla uses
            //The overlay then fades out onto an already built screen instead of revealing the empty background first
            _screens?.SetScreen(new TitleScreen());
            _screens?.SetOverlay(CreateStartupOverlay());
            _gpuApp.FrameTick += OnGameTick;
            _gpuApp.GuiTick += OnGuiTick;
            try
            {
                _gpuApp.Run();
            }
            finally
            {
                _gpuApp.FrameTick -= OnGameTick;
                _gpuApp.GuiTick -= OnGuiTick;
                _running = false;
                Log.Info($"MinecraftClient window-driven loop exited, total frames {_frameCount}");
            }
            return;
        }
        Log.Info($"MinecraftClient headless mode started render distance {Config.RenderDistance} FOV {Config.Fov}");
        var watch = Stopwatch.StartNew();
        try
        {
            while (_running)
            {
                var frameStart = watch.Elapsed;
                GameTick(TargetFrameMillis / 1000.0);
                if (_sleepBudgetMillis > 0)
                {
                    var elapsed = (int)(watch.Elapsed - frameStart).TotalMilliseconds;
                    var remaining = _sleepBudgetMillis - elapsed;
                    if (remaining > 0) Thread.Sleep(remaining);
                }
            }
        }
        finally
        {
            _running = false;
            Log.Info($"MinecraftClient headless loop exited, total frames {_frameCount}");
        }
    }

    //OnGameTick runs on the Tick thread: world logic only
    //The GUI is not touched here, so the tick rate never gates how often the picture changes
    private void OnGameTick(double delta) => GameTick(delta);

    //OnGuiTick runs on the Render thread: input, layout, widget animation and screen updates
    //Vanilla updates widgets inside renderWidget for the same reason, so hover and animation track the frame rate
    private void OnGuiTick(double delta) => GuiTick(delta);

    //GameTick world logic, decoupled from the GUI since the render thread owns the widget tree
    private void GameTick(double delta)
    {
        //Queued work runs early in the tick, the same place vanilla Minecraft.runTick calls runAllTasks
        RunQueuedTasks();
        _connection?.Tick();
        //The local world advances one frame after packet processing, converting this frame to a number of ticks by the server-sent tick rate
        //When the server is frozen it does not advance ticks, so entity animation stops
        _level.Tick(delta);
        //Sky brightness currently has no client time source, constant noon; it will be computed from time once time sync is added
        _lightTexture?.Update(1.0f);
        UpdateCamera();
        _frameCount++;
    }

    //GuiTick input dispatch, layout and control animation for one rendered frame
    private void GuiTick(double delta)
    {
        //Screen switches queued by game logic land before input, so a frame never sees a half-replaced tree
        while (_guiTasks.TryDequeue(out var task))
        {
            try
            {
                task();
            }
            catch (Exception ex)
            {
                Log.Warning($"Queued gui task failed: {ex}");
            }
        }
        _gpuApp?.PollInput();
        _screens?.ProcessLayoutIfDirty();
        _gpuApp?.Window.Update(delta);
        _screens?.Tick();
    }

    //UpdateCamera updates the camera from the local player position/orientation and the perspective from Config.Fov/RenderDistance
    //Before the player position is synced, uses a default spawn-point top-down view so there is something on screen at start
    private void UpdateCamera()
    {
        if (_worldRenderer is null) return;
        var player = Player;
        _camera.SetPosition(new Vector3((float)player.Pos.X, (float)player.Pos.Y, (float)player.Pos.Z));
        _camera.SetRotation(player.YRot, player.XRot);
        var width = _gpuApp?.Window.SurfaceWidth ?? 800;
        var height = _gpuApp?.Window.SurfaceHeight ?? 600;
        var fov = Config.Fov * MathF.PI / 180f;
        var zFar = Math.Max(64f, Config.RenderDistance * 16f * 2f);
        _camera.UpdatePerspective(fov, width, height, 0.05f, zFar);
    }

    //Stop triggers main loop exit, called by window close or a ShutdownHook
    //In window-driven mode calls gpuApp.RequestClose so _window.Run exits
    public void Stop()
    {
        Log.Info("MinecraftClient received stop signal");
        _running = false;
        _shutdownCts.Cancel();
        _gpuApp?.RequestClose();
    }

    public void Dispose()
    {
        if (_gpuApp is not null && _screens is not null)
        {
            _gpuApp.RawKeyDown -= _screens.HandleRawKeyDown;
            _gpuApp.RawMouseDown -= _screens.HandleRawMouseDown;
            _gpuApp.RawMouseUp -= _screens.HandleRawMouseUp;
            _gpuApp.RawMouseMove -= _screens.HandleRawMouseMove;
        }
        _shutdownCts.Dispose();
        _worldRenderer?.Dispose();
        _worldDispatcher?.Dispose();
        _connection?.Dispose();
        _gpuApp?.Dispose();
    }
}
