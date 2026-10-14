using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace VybeShift.Core.Audio;

/// <summary>
/// Enumerates and monitors Windows audio endpoints.
/// Notifies of hot-plug events via IMMNotificationClient.
/// </summary>
public sealed class DeviceManager : IMMNotificationClient, IDisposable
{
    private readonly MMDeviceEnumerator _enum = new();

    public event Action? DeviceListChanged;
    public event Action<string>? DeviceRemoved;
    public event Action<string>? DeviceAdded;

    public DeviceManager()
    {
        _enum.RegisterEndpointNotificationCallback(this);
    }

    public IReadOnlyList<AudioDeviceInfo> GetInputDevices()  => Enumerate(DataFlow.Capture);
    public IReadOnlyList<AudioDeviceInfo> GetOutputDevices() => Enumerate(DataFlow.Render);

    public MMDevice? GetById(string id)
    {
        try { return _enum.GetDevice(id); } catch { return null; }
    }

    public MMDevice? DefaultInput()
    {
        try { return _enum.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications); } catch { return null; }
    }

    public MMDevice? DefaultOutput()
    {
        try { return _enum.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia); } catch { return null; }
    }

    /// <summary>Finds any VB-Cable render endpoint for virtual mic piping.</summary>
    public MMDevice? FindVBCable()
    {
        try
        {
            return _enum.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                        .FirstOrDefault(d => d.FriendlyName.Contains("CABLE", StringComparison.OrdinalIgnoreCase));
        }
        catch { return null; }
    }

    private IReadOnlyList<AudioDeviceInfo> Enumerate(DataFlow flow)
    {
        var list = new List<AudioDeviceInfo>();
        try
        {
            foreach (var d in _enum.EnumerateAudioEndPoints(flow, DeviceState.Active))
            {
                try
                {
                    var f = d.AudioClient.MixFormat;
                    list.Add(new AudioDeviceInfo(d.ID, d.FriendlyName, f.Channels, f.SampleRate, f.BitsPerSample));
                }
                catch { }
            }
        }
        catch { }
        return list;
    }

    // IMMNotificationClient
    void IMMNotificationClient.OnDeviceAdded(string id)    { DeviceAdded?.Invoke(id);   DeviceListChanged?.Invoke(); }
    void IMMNotificationClient.OnDeviceRemoved(string id)  { DeviceRemoved?.Invoke(id); DeviceListChanged?.Invoke(); }
    void IMMNotificationClient.OnDefaultDeviceChanged(DataFlow f, Role r, string id) => DeviceListChanged?.Invoke();
    void IMMNotificationClient.OnDeviceStateChanged(string id, DeviceState s) => DeviceListChanged?.Invoke();
    void IMMNotificationClient.OnPropertyValueChanged(string id, PropertyKey k) { }

    public void Dispose()
    {
        try { _enum.UnregisterEndpointNotificationCallback(this); } catch { }
        _enum.Dispose();
    }
}
