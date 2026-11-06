namespace NacreCraft.Client.Blaze3d.Audio;

//AbstractDeviceTracker shared device-list refresh state, maps to vanilla com.mojang.blaze3d.audio.AbstractDeviceTracker
public abstract class AbstractDeviceTracker : DeviceTracker
{
    private volatile DeviceList _deviceList;
    private readonly object _updateLock = new();
    private bool _updatePending;

    protected AbstractDeviceTracker(DeviceList deviceList) => _deviceList = deviceList;

    protected abstract bool IsUpdateRequested();

    protected abstract void DiscardUpdateRequest();

    public DeviceList CurrentDevices() => _deviceList;

    public void ForceRefresh()
    {
        DiscardUpdateRequest();
        _deviceList = DeviceList.Query();
    }

    public void Tick()
    {
        if (!IsUpdateRequested()) return;
        DiscardUpdateRequest();

        lock (_updateLock)
        {
            if (_updatePending) return;
            _updatePending = true;
        }

        _ = Task.Run(() =>
        {
            try
            {
                _deviceList = DeviceList.Query();
            }
            finally
            {
                lock (_updateLock) _updatePending = false;
            }
        });
    }
}
