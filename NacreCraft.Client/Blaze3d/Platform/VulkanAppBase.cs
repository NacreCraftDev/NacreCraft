using System.Numerics;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;
using SilkWindow = Silk.NET.Windowing.Window;
using Image = Silk.NET.Vulkan.Image;
using NacreCraft.Client.Blaze3d.Systems;
using NacreCraft.Client.Blaze3d.Textures;
using NacreCraft.Client.Blaze3d.Shaders;
using NacreCraft.Client.Blaze3d.Vulkan;
using NacreCraft.Client.Render;
using NacreCraft.Logging;

namespace NacreCraft.Client.Blaze3d.Platform;

//VulkanAppBase common base class for the Vulkan sample apps
//Owns the window, the backend, the logical device and the surface; subclasses record their rendering into an offscreen target
//The frame loop acquires a swapchain image, records into the scene texture, blits it to the swapchain and presents
public abstract unsafe class VulkanAppBase : IDisposable
{
    protected IWindow _window = null!;
    protected VulkanBackend _backend = null!;
    protected VulkanDevice _vkDevice = null!;
    protected GpuDevice _device = null!;
    protected VulkanGpuSurface _vkSurface = null!;
    protected GpuSurface _surface = null!;
    protected CommandEncoder _encoder = null!;
    protected GpuTexture _sceneColor = null!;
    protected GpuTextureView _sceneView = null!;
    protected Format _swapchainImageFormat;
    protected Extent2D _swapchainExtent;
    protected uint _framesRendered;
    private int _maxFrames = -1;
    protected bool _disposed;
    protected bool _initialized;
    //The window-size-changed flag is set by the Resize event; DrawFrame detects it and triggers swapchain recreation
    protected bool _framebufferResized;
    //EnableVsync whether vsync is enabled, true by default; subclasses set it from GameConfig at construction
    protected bool EnableVsync = true;
    //StartFullscreen whether the window opens fullscreen; subclasses set it from GameConfig at construction
    protected bool StartFullscreen;
    //Frame rate report state, debug only
    private long _lastFrameReportTicks;
    private int _framesSinceReport;
    //_preferredVideoMode the fullscreen video mode asked for, null means the monitor's current mode
    private VideoMode? _preferredVideoMode;
    //_videoModeDirty set when the preferred mode changes, applied at the top of the next frame
    private bool _videoModeDirty;
    //_fullscreen is the wanted state that ToggleFullScreen flips, _actuallyFullscreen the one the window is in
    //Keeping them apart means a window change only ever happens at the top of a frame, maps to vanilla Window
    private bool _fullscreen;
    private bool _actuallyFullscreen;
    //Windowed position and size, kept while fullscreen so leaving it restores where the window was
    private Vector2D<int> _windowedPosition;
    private Vector2D<int> _windowedSize;

    private readonly ShaderManager _shaderManager = new();
    private GpuSurface.PresentMode _presentMode;

    public int SurfaceWidth { get; }
    public int SurfaceHeight { get; }
    public Vk Api => _vkDevice.Api;

    protected VulkanAppBase(int width, int height)
    {
        SurfaceWidth = width;
        SurfaceHeight = height;
    }

    //WindowTitle window title provided by subclasses
    protected abstract string WindowTitle { get; }

    //OnCreatePipelineResources subclasses create pipelines and dependent resources
    //_vkDevice/_swapchainImageFormat/_swapchainExtent are ready at this point
    protected abstract void OnCreatePipelineResources();

    //OnRecordCommandBuffer subclasses record their rendering into the scene texture
    protected abstract void OnRecordCommandBuffer(CommandEncoder encoder, GpuTextureView sceneView);

    //OnSwapchainRecreated after swapchain recreation subclasses can rebuild extent-dependent resources like the pipeline viewport
    protected virtual void OnSwapchainRecreated() { }

    //OnCleanupPipelineResources subclasses clean up their own pipeline resources while the device is not yet destroyed
    protected virtual void OnCleanupPipelineResources() { }

    //OnInitialized fired after the window and Vulkan resources are initialized; subclasses may override to subscribe to input events
    protected virtual void OnInitialized() { }

    //OnBeforeRun hook before the main loop's _window.Run; subclasses may start background tasks like the Tick thread
    protected virtual void OnBeforeRun() { }

    //OnAfterRun hook after the main loop's _window.Run exits; subclasses may join background threads and propagate exceptions
    protected virtual void OnAfterRun() { }

    public void Run()
    {
        Init();
        OnInitialized();
        _window.Render += DrawFrame;
        _window.Resize += OnWindowResize;
        OnBeforeRun();
        try
        {
            _window.Run();
        }
        finally
        {
            try
            {
                OnAfterRun();
            }
            finally
            {
                _vkDevice.WaitIdle();
                Cleanup();
            }
        }
    }

    //RequestClose requests the window to close so _window.Run exits the loop, called externally by MinecraftClient.Stop
    public void RequestClose()
    {
        if (_initialized) _window.Close();
    }

    //RunFor starts the main loop and exits automatically after the given number of frames
    public void RunFor(int maxFrames)
    {
        _maxFrames = maxFrames;
        Run();
    }

    private void Init()
    {
        InitWindow();
        InitVulkan();
        _initialized = true;
    }

    private void InitWindow()
    {
        var opts = WindowOptions.DefaultVulkan;
        opts.Size = new Vector2D<int>(SurfaceWidth, SurfaceHeight);
        opts.Title = WindowTitle;
        //Opening fullscreen is the same wanted state a toggle produces, so it goes through the same flag pair
        if (StartFullscreen)
        {
            _fullscreen = true;
            _actuallyFullscreen = true;
            opts.WindowState = WindowState.Fullscreen;
        }
        _window = SilkWindow.Create(opts);
        _window.Initialize();
        if (_window.VkSurface is null)
        {
            throw new NotSupportedException("The platform does not support Vulkan");
        }
    }

    private void InitVulkan()
    {
        _backend = new VulkanBackend(_window);
        CreateSurface();
        _backend.PickPhysicalDevice();
        _vkDevice = new VulkanDevice(_backend, _shaderManager, GpuDebugOptions.None);
        _device = new GpuDevice(_vkDevice, _shaderManager, () => { });
        _vkSurface = (VulkanGpuSurface)_vkDevice.CreateSurface(0);
        _surface = new GpuSurface(_vkSurface);
        _presentMode = GpuSurface.GetSupportedVsyncMode(_vkSurface.SupportedPresentModes(), EnableVsync);
        //Configure through the wrapper, not the backend: AcquireNextTexture goes through the wrapper and checks
        //that a configuration was recorded, which only the wrapper keeps
        _surface.Configure(new GpuSurface.Configuration(SurfaceWidth, SurfaceHeight, _presentMode));
        _encoder = _device.CreateCommandEncoder();
        CreateSceneResources();
        OnCreatePipelineResources();
    }

    //CreateSurface creates the platform surface and injects it into the backend
    protected virtual void CreateSurface()
    {
        var surfaceHandle = _window.VkSurface!.Create<AllocationCallbacks>(_backend.Instance.ToHandle(), null);
        _backend.Surface = surfaceHandle.ToSurface();
    }

    //CreateSceneResources builds the offscreen target every app renders into before blitting to the swapchain
    private void CreateSceneResources()
    {
        _swapchainImageFormat = _vkSurface.ImageFormat;
        _swapchainExtent = _vkSurface.Extent;
        _sceneColor = _device.CreateTexture(
            "scene",
            GpuTexture.UsageRenderAttachment | GpuTexture.UsageCopySrc | GpuTexture.UsageCopyDst,
            //Must match ColorTargetState.DEFAULT, the format every client pipeline declares as its color target
            GpuFormat.Rgba8Unorm,
            (int)_swapchainExtent.Width,
            (int)_swapchainExtent.Height,
            1,
            1);
        _sceneView = _device.CreateTextureView(_sceneColor);
    }

    //OnWindowResize sets the flag when the window size changes; DrawFrame detects it at the end and recreates the swapchain
    private void OnWindowResize(Vector2D<int> obj)
    {
        _framebufferResized = true;
    }

    //ToggleFullScreen flips the wanted fullscreen state only, maps to vanilla Window.toggleFullScreen
    //The window itself is not touched here: UpdateFullscreenIfChanged applies it at the top of a frame, where no
    //swapchain image is held, which is what keeps a toggle from racing the acquired image
    public void ToggleFullScreen() => _fullscreen = !_fullscreen;

    //UpdateFullscreenIfChanged applies a pending toggle, maps to vanilla Window.updateFullscreenIfChanged
    private void UpdateFullscreenIfChanged()
    {
        if (_fullscreen == _actuallyFullscreen) return;
        _actuallyFullscreen = _fullscreen;
        try
        {
            ApplyWindowMode();
        }
        catch (Exception ex)
        {
            Log.Warning($"Couldn't toggle fullscreen: {ex.Message}");
        }
    }

    //SetPreferredFullscreenVideoMode records the wanted fullscreen video mode, maps to vanilla Window.setPreferredFullscreenVideoMode
    //The window is not touched here: ChangeFullscreenVideoMode applies it at the top of a frame, where no image is held
    public void SetPreferredFullscreenVideoMode(VideoMode? mode)
    {
        if (mode == _preferredVideoMode) return;
        _preferredVideoMode = mode;
        _videoModeDirty = true;
    }

    //ChangeFullscreenVideoMode applies a pending video mode change while fullscreen
    //maps to vanilla Window.changeFullscreenVideoMode
    private void ChangeFullscreenVideoMode()
    {
        if (!_actuallyFullscreen || !_videoModeDirty) return;
        _videoModeDirty = false;
        try
        {
            ApplyWindowMode();
        }
        catch (Exception ex)
        {
            Log.Warning($"Couldn't change fullscreen video mode: {ex.Message}");
        }
    }

    //ApplyWindowMode puts the window into the wanted mode, maps to vanilla Window.setMode
    //Leaving fullscreen restores the position and size the window had on entry, which is why they get captured here
    private void ApplyWindowMode()
    {
        if (_fullscreen)
        {
            _windowedPosition = _window.Position;
            _windowedSize = _window.Size;
            //A named mode is recorded and re-applied whenever it changes, but the window is put into the monitor's
            //current mode: Silk.NET does not expose the GLFW handle in a form that takes an explicit mode, and with no
            //video settings screen yet there is nothing to pick one from, so an exact mode waits on that
            _window.WindowState = WindowState.Fullscreen;
        }
        else
        {
            _window.WindowState = WindowState.Normal;
            _window.Position = _windowedPosition;
            _window.Size = _windowedSize;
        }
        //The mode change resizes the framebuffer, so ask for a rebuild rather than waiting on the resize event
        _framebufferResized = true;
    }

    //RecreateSwapchain rebuilds the swapchain and dependent resources on a window size change or swapchain invalidation
    protected void RecreateSwapchain()
    {
        _vkDevice.WaitIdle();
        var fb = _window.FramebufferSize;
        if (fb.X <= 0 || fb.Y <= 0)
        {
            //Size 0 keeps the flag to avoid being unable to rebuild after the cleanup and looping forever
            Log.Debug($"swapchain recreate skipped: framebuffer is {fb.X}x{fb.Y}");
            _framebufferResized = true;
            return;
        }
        _sceneView.Dispose();
        _sceneColor.Dispose();
        _surface.Configure(new GpuSurface.Configuration(fb.X, fb.Y, _presentMode));
        CreateSceneResources();
        OnSwapchainRecreated();
    }

    private void DrawFrame(double obj)
    {
        //Debug timing: the frame rate is reported once a second, which is enough to tell a real drop from a one-off hitch
        if (Log.DebugEnabled)
        {
            var nowTicks = Environment.TickCount64;
            if (_lastFrameReportTicks == 0) _lastFrameReportTicks = nowTicks;
            _framesSinceReport++;
            if (nowTicks - _lastFrameReportTicks >= 1000)
            {
                Log.Debug($"frame rate {_framesSinceReport * 1000 / (nowTicks - _lastFrameReportTicks)} fps");
                _framesSinceReport = 0;
                _lastFrameReportTicks = nowTicks;
            }
        }
        //Window changes land here, before the surface is touched, so a fullscreen toggle can never happen while an
        //image is acquired; vanilla makes the same call at the top of its frame
        UpdateFullscreenIfChanged();
        //A video mode change is a window change too, so it belongs in the same place
        ChangeFullscreenVideoMode();
        //The surface has to be reconfigured before anything is acquired: GpuSurface refuses a configure while an image
        //is held, and a fullscreen toggle invalidates the swapchain out from under that image
        if (_framebufferResized || _vkSurface.IsSuboptimal)
        {
            _framebufferResized = false;
            //A rebuild does not skip the frame: the swapchain is fresh right after, and drawing into it at once is what
            //keeps a fullscreen toggle from leaving the window empty while the new targets settle
            RecreateSwapchain();
        }
        try
        {
            //Descriptor sets are handed out on every pipeline bind and never returned individually, so the pool is
            //reset once per frame; the previous frame's sets are idle because submits wait synchronously
            _vkDevice.ResetDescriptorPool();
            _surface.AcquireNextTexture();
        }
        catch (SurfaceException)
        {
            //Acquire leaves the surface unacquired when it throws, so the rebuild is deferred to the next frame to
            //keep configure and acquire in that order
            Log.Debug("swapchain recreate deferred: acquire reported the surface is out of date");
            _framebufferResized = true;
            return;
        }

        var vkEncoder = (VulkanCommandEncoder)_encoder.Backend;
        vkEncoder.BeginRecording();
        OnRecordCommandBuffer(_encoder, _sceneView);
        _surface.BlitFromTexture(_encoder, _sceneView);
        vkEncoder.Submit();
        _surface.Present();

        if (_vkSurface.IsSuboptimal || _framebufferResized)
        {
            _framebufferResized = false;
            RecreateSwapchain();
        }

        _framesRendered++;
        //The window framebuffer and the swapchain extent must agree, otherwise what is presented is not what the window shows
        if (_framesRendered % 120 == 0)
        {
            var fbSize = _window.FramebufferSize;
            Log.Debug($"presented frame {_framesRendered} framebuffer={fbSize.X}x{fbSize.Y} swapchain={_swapchainExtent.Width}x{_swapchainExtent.Height} format={_swapchainImageFormat}");
        }
        if (_maxFrames > 0 && _framesRendered >= _maxFrames)
        {
            _window.Close();
        }
    }

    private void Cleanup()
    {
        if (!_initialized) return;
        _initialized = false;
        _vkDevice.WaitIdle();
        OnCleanupPipelineResources();
        _sceneView?.Dispose();
        _sceneColor?.Dispose();
        _encoder?.Dispose();
        _surface?.Dispose();
        _device?.Dispose();
        _backend?.Dispose();
        //Clears event subscriptions so callbacks during Reset do not access released resources
        _window.Render -= DrawFrame;
        _window.Resize -= OnWindowResize;
    }

    public virtual void Dispose()
    {
        if (_disposed) return;
        Cleanup();
        //Silk.NET's Dispose internally calls Reset; after a long run it may still report being inside the render loop and throw
        //Vulkan resources and event subscriptions were already released in Cleanup; this third-party error is ignored
        //A window that never came up leaves this null, and the failure that kept it null is the one worth reporting; throwing
        //from the cleanup path would replace it with a null reference that says nothing about the real cause
        try { _window?.Dispose(); }
        catch (InvalidOperationException) { }
        _disposed = true;
    }
}
