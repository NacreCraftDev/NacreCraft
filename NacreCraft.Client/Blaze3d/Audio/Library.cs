using NacreCraft.Logging;
using Silk.NET.OpenAL;
using SilkAL = Silk.NET.OpenAL.AL;
using SilkALContext = Silk.NET.OpenAL.ALContext;

namespace NacreCraft.Client.Blaze3d.Audio;

//Library owns the OpenAL device, context and channel pools, maps to vanilla com.mojang.blaze3d.audio.Library
public sealed unsafe class Library : IDisposable
{
    public const int NoDevice = 0;
    private const string NoDeviceName = "(None)";
    private const int DefaultChannelCount = 30;

    private nint _currentDevice;
    private nint _context;
    private string _currentDeviceName = NoDeviceName;
    private bool _supportsDisconnections;
    private SilkAL? _al;
    private SilkALContext? _alc;
    private ChannelPool _staticChannels = EmptyPool.Instance;
    private ChannelPool _streamingChannels = EmptyPool.Instance;
    private readonly Listener _listener = new();

    public void Init(string? preferredDevice, DeviceList currentDevices, bool useHrtf)
    {
        _currentDeviceName = NoDeviceName;
        _alc = SilkALContext.GetApi(true);
        OpenAlUtil.Alc = _alc;

        _currentDevice = OpenDeviceOrFallback(preferredDevice, currentDevices.DefaultDevice);
        var queriedName = _alc.GetContextProperty((Device*)_currentDevice, GetContextString.DeviceSpecifier);
        _currentDeviceName = queriedName ?? NoDeviceName;

        var attributes = stackalloc int[11];
        var cursor = 0;
        var numHrtf = 0;
        _alc.GetContextProperty((Device*)_currentDevice, (GetContextInteger)6548, 1, &numHrtf);
        if (numHrtf > 0)
        {
            attributes[cursor++] = 6546;
            attributes[cursor++] = useHrtf ? 1 : 0;
            attributes[cursor++] = 6550;
            attributes[cursor++] = 0;
        }
        attributes[cursor++] = 6554;
        attributes[cursor++] = 1;
        attributes[cursor] = 0;

        _context = (nint)_alc.CreateContext((Device*)_currentDevice, attributes);
        if (OpenAlUtil.CheckALCError(_currentDevice, "Create context"))
            throw new InvalidOperationException("Unable to create OpenAL context");
        if (!_alc.MakeContextCurrent((Context*)_context))
            throw new InvalidOperationException("Unable to make the OpenAL context current");

        var totalChannelCount = GetChannelCount();
        var streamingChannelCount = Math.Clamp((int)Math.Sqrt(totalChannelCount), 2, 8);
        var staticChannelCount = Math.Clamp(totalChannelCount - streamingChannelCount, 8, 255);
        _staticChannels = new CountingChannelPool(staticChannelCount);
        _streamingChannels = new CountingChannelPool(streamingChannelCount);

        _al = SilkAL.GetApi(true);
        OpenAlUtil.Al = _al;
        OpenAlUtil.CheckALError("Initialization");
        if (!_al.IsExtensionPresent("AL_EXT_source_distance_model"))
            throw new InvalidOperationException("AL_EXT_source_distance_model is not supported");
        _al.Enable((Capability)512);
        if (!_al.IsExtensionPresent("AL_EXT_LINEAR_DISTANCE"))
            throw new InvalidOperationException("AL_EXT_LINEAR_DISTANCE is not supported");
        OpenAlUtil.CheckALError("Enable per-source distance models");

        Log.Info($"OpenAL initialized on device {_currentDeviceName}");
        _supportsDisconnections = _alc.IsExtensionPresent((Device*)_currentDevice, "ALC_EXT_disconnect");
    }

    public string? CurrentDeviceName() => _currentDeviceName;

    public Listener GetListener() => _listener;

    public Channel? AcquireChannel(Pool pool)
        => (pool == Pool.STREAMING ? _streamingChannels : _staticChannels).Acquire();

    public void ReleaseChannel(Channel channel)
    {
        if (!_staticChannels.Release(channel) && !_streamingChannels.Release(channel))
            throw new InvalidOperationException("Tried to release unknown channel");
    }

    public string GetChannelDebugString()
        => $"Sounds: {_staticChannels.UsedCount}/{_staticChannels.MaxCount} "
           + $"+ {_streamingChannels.UsedCount}/{_streamingChannels.MaxCount}";

    public bool IsCurrentDeviceDisconnected()
    {
        if (!_supportsDisconnections || _alc is null) return false;
        var connected = 0;
        _alc.GetContextProperty((Device*)_currentDevice, (GetContextInteger)787, 1, &connected);
        return connected == 0;
    }

    public static DeviceTracker CreateDeviceTracker()
    {
        var deviceList = DeviceList.Query();
        if (!CallbackDeviceTracker.IsSupported()) return new PollingDeviceTracker(deviceList);
        Log.Debug("Using SOFT_system_events callback for tracking audio device changes");
        return CallbackDeviceTracker.CreateAndInstall(deviceList);
    }

    private nint OpenDeviceOrFallback(string? preferredDevice, string? systemDefaultDevice)
    {
        if (preferredDevice is not null && TryOpenDevice(preferredDevice) is { } preferred)
            return preferred;
        if (systemDefaultDevice is not null && TryOpenDevice(systemDefaultDevice) is { } systemDefault)
            return systemDefault;
        if (TryOpenDevice(null) is { } fallback)
            return fallback;
        throw new InvalidOperationException("Failed to open OpenAL device");
    }

    private nint? TryOpenDevice(string? name)
    {
        var device = (nint)_alc!.OpenDevice(name);
        if (device == 0 || OpenAlUtil.CheckALCError(device, "Open device")) return null;
        return device;
    }

    private int GetChannelCount()
    {
        var size = 0;
        _alc!.GetContextProperty((Device*)_currentDevice, GetContextInteger.AttributesSize, 1, &size);
        if (OpenAlUtil.CheckALCError(_currentDevice, "Get attributes size"))
            throw new InvalidOperationException("Failed to get OpenAL attributes");

        var attributes = stackalloc int[size];
        _alc.GetContextProperty((Device*)_currentDevice, GetContextInteger.AllAttributes, size, attributes);
        if (OpenAlUtil.CheckALCError(_currentDevice, "Get attributes"))
            throw new InvalidOperationException("Failed to get OpenAL attributes");

        for (var pos = 0; pos < size;)
        {
            var attribute = attributes[pos++];
            if (attribute == 0) break;
            var value = attributes[pos++];
            if (attribute == 4112) return value;
        }
        return DefaultChannelCount;
    }

    public void Cleanup()
    {
        _staticChannels.Cleanup();
        _streamingChannels.Cleanup();
        if (_alc is not null && _context != 0)
        {
            _alc.DestroyContext((Context*)_context);
            _context = 0;
        }
        if (_alc is not null && _currentDevice != 0)
        {
            _alc.CloseDevice((Device*)_currentDevice);
            _currentDevice = 0;
        }
        if (_al is IDisposable disposableAl) disposableAl.Dispose();
        if (_alc is IDisposable disposableAlc) disposableAlc.Dispose();
        _al = null;
        _alc = null;
        OpenAlUtil.Al = null;
        OpenAlUtil.Alc = null;
    }

    public void Dispose() => Cleanup();

    public enum Pool
    {
        STATIC,
        STREAMING
    }

    private interface ChannelPool
    {
        Channel? Acquire();

        bool Release(Channel channel);

        void Cleanup();

        int MaxCount { get; }

        int UsedCount { get; }
    }

    private sealed class EmptyPool : ChannelPool
    {
        public static readonly EmptyPool Instance = new();

        public Channel? Acquire() => null;

        public bool Release(Channel channel) => false;

        public void Cleanup()
        {
        }

        public int MaxCount => 0;

        public int UsedCount => 0;
    }

    private sealed class CountingChannelPool(int limit) : ChannelPool
    {
        private readonly HashSet<Channel> _activeChannels = new(ReferenceEqualityComparer.Instance);

        public Channel? Acquire()
        {
            if (_activeChannels.Count >= limit) return null;
            var channel = Channel.Create();
            if (channel is not null) _activeChannels.Add(channel);
            return channel;
        }

        public bool Release(Channel channel)
        {
            if (!_activeChannels.Remove(channel)) return false;
            channel.Destroy();
            return true;
        }

        public void Cleanup()
        {
            foreach (var channel in _activeChannels) channel.Destroy();
            _activeChannels.Clear();
        }

        public int MaxCount => limit;

        public int UsedCount => _activeChannels.Count;
    }
}
