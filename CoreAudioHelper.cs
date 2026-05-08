using System.Runtime.InteropServices;

namespace ACU624KeyMapper;

/// <summary>
/// Finds the Windows audio render and capture endpoints for the ACU-624D
/// (matching VID_0000&PID_3200 in the device instance ID) and monitors their
/// master volume using the Core Audio IAudioEndpointVolume COM interface.
/// </summary>
public sealed class CoreAudioHelper : IDisposable
{
    // ── COM GUIDs ─────────────────────────────────────────────────────────
    private static readonly Guid CLSID_MMDeviceEnumerator = new("BCDE0395-E52F-467C-8E3D-C4579291692E");
    private static readonly Guid IID_IAudioEndpointVolume  = new("5CDF2C82-841E-4546-9722-0CF74078229A");
    private static readonly Guid IID_IMMDevice             = new("D666063F-1587-4E43-81F1-B948E807363F");

    // ── COM interfaces (minimal vtable declarations) ──────────────────────
    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        [PreserveSig] int EnumAudioEndpoints(int dataFlow, int stateMask,
            [MarshalAs(UnmanagedType.Interface)] out IMMDeviceCollection ppDevices);
        [PreserveSig] int GetDefaultAudioEndpoint(int dataFlow, int role,
            [MarshalAs(UnmanagedType.Interface)] out IMMDevice ppEndpoint);
        [PreserveSig] int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string pwstrId,
            [MarshalAs(UnmanagedType.Interface)] out IMMDevice ppDevice);
        [PreserveSig] int RegisterEndpointNotificationCallback(IntPtr pNotify);
        [PreserveSig] int UnregisterEndpointNotificationCallback(IntPtr pNotify);
    }

    [ComImport, Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceCollection
    {
        [PreserveSig] int GetCount(out uint pcDevices);
        [PreserveSig] int Item(uint nDevice,
            [MarshalAs(UnmanagedType.Interface)] out IMMDevice ppDevice);
    }

    [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        [PreserveSig] int Activate(ref Guid iid, uint dwClsCtx, IntPtr pActivationParams, out IntPtr ppInterface);
        [PreserveSig] int OpenPropertyStore(uint stgmAccess, out IntPtr ppProperties);
        [PreserveSig] int GetId([MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);
        [PreserveSig] int GetState(out uint pdwState);
    }

    [ComImport, Guid("5CDF2C82-841E-4546-9722-0CF74078229A"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioEndpointVolume
    {
        [PreserveSig] int RegisterControlChangeNotify(IntPtr pNotify);
        [PreserveSig] int UnregisterControlChangeNotify(IntPtr pNotify);
        [PreserveSig] int GetChannelCount(out uint pnChannelCount);
        [PreserveSig] int SetMasterVolumeLevel(float fLevelDB, ref Guid pguidEventContext);
        [PreserveSig] int SetMasterVolumeLevelScalar(float fLevel, ref Guid pguidEventContext);
        [PreserveSig] int GetMasterVolumeLevel(out float pfLevelDB);
        [PreserveSig] int GetMasterVolumeLevelScalar(out float pfLevel);
        [PreserveSig] int SetChannelVolumeLevel(uint nChannel, float fLevelDB, ref Guid pguidEventContext);
        [PreserveSig] int SetChannelVolumeLevelScalar(uint nChannel, float fLevel, ref Guid pguidEventContext);
        [PreserveSig] int GetChannelVolumeLevel(uint nChannel, out float pfLevelDB);
        [PreserveSig] int GetChannelVolumeLevelScalar(uint nChannel, out float pfLevel);
        [PreserveSig] int SetMute([MarshalAs(UnmanagedType.Bool)] bool bMute, ref Guid pguidEventContext);
        [PreserveSig] int GetMute([MarshalAs(UnmanagedType.Bool)] out bool pbMute);
        // ... remaining methods not needed
    }

    // ── State ─────────────────────────────────────────────────────────────
    private IAudioEndpointVolume? _renderVol;
    private IAudioEndpointVolume? _captureVol;
    private readonly string _vidPidFilter;

    public bool IsAvailable => _renderVol != null || _captureVol != null;

    public CoreAudioHelper(ushort vid, ushort pid)
    {
        _vidPidFilter = $"VID_{vid:X4}&PID_{pid:X4}".ToUpperInvariant();
        Initialise();
    }

    private void Initialise()
    {
        try
        {
            var enumeratorType = Type.GetTypeFromCLSID(CLSID_MMDeviceEnumerator)!;
            var enumerator = (IMMDeviceEnumerator)Activator.CreateInstance(enumeratorType)!;

            // 0 = eRender (playback), 1 = eCapture (recording), 2 = eAll
            // DEVICE_STATE_ACTIVE = 1
            _renderVol  = FindEndpointVolume(enumerator, 0);
            _captureVol = FindEndpointVolume(enumerator, 1);

            Marshal.ReleaseComObject(enumerator);
        }
        catch { /* Core Audio unavailable */ }
    }

    private IAudioEndpointVolume? FindEndpointVolume(IMMDeviceEnumerator enumerator, int dataFlow)
    {
        if (enumerator.EnumAudioEndpoints(dataFlow, 1 /*ACTIVE*/, out var collection) != 0) return null;
        collection.GetCount(out uint count);

        for (uint i = 0; i < count; i++)
        {
            if (collection.Item(i, out var device) != 0) continue;
            device.GetId(out string id);

            if (id.ToUpperInvariant().Contains(_vidPidFilter))
            {
                Guid iid = IID_IAudioEndpointVolume;
                if (device.Activate(ref iid, 0x17 /*CLSCTX_ALL*/, IntPtr.Zero, out IntPtr volPtr) == 0)
                {
                    Marshal.ReleaseComObject(collection);
                    Marshal.ReleaseComObject(device);
                    return (IAudioEndpointVolume)Marshal.GetObjectForIUnknown(volPtr);
                }
            }
            Marshal.ReleaseComObject(device);
        }

        Marshal.ReleaseComObject(collection);
        return null;
    }

    /// <summary>Returns headset output volume 0-100, or -1 if unavailable.</summary>
    public int GetOutputVolume()
    {
        if (_renderVol == null) return -1;
        try
        {
            _renderVol.GetMasterVolumeLevelScalar(out float level);
            _renderVol.GetMute(out bool muted);
            return muted ? 0 : (int)Math.Round(level * 100);
        }
        catch { return -1; }
    }

    /// <summary>Returns microphone input volume 0-100, or -1 if unavailable.</summary>
    public int GetInputVolume()
    {
        if (_captureVol == null) return -1;
        try
        {
            _captureVol.GetMasterVolumeLevelScalar(out float level);
            _captureVol.GetMute(out bool muted);
            return muted ? 0 : (int)Math.Round(level * 100);
        }
        catch { return -1; }
    }

    public void Dispose()
    {
        if (_renderVol  != null) { try { Marshal.ReleaseComObject(_renderVol);  } catch { } }
        if (_captureVol != null) { try { Marshal.ReleaseComObject(_captureVol); } catch { } }
    }
}
