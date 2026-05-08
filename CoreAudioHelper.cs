using System.Runtime.InteropServices;

namespace ACU624KeyMapper;

/// <summary>
/// Finds the Windows audio render and capture endpoints for the ACU-624D and
/// monitors their master volume using IAudioEndpointVolume.
///
/// IMMDevice.GetId() returns an opaque endpoint ID that does NOT contain
/// VID/PID, so we open the property store and read PKEY_Device_InstanceId
/// (which looks like "USB\VID_0000&PID_3200\...") to match the device.
/// </summary>
public sealed class CoreAudioHelper : IDisposable
{
    // ── COM GUIDs ─────────────────────────────────────────────────────────
    private static readonly Guid CLSID_MMDeviceEnumerator = new("BCDE0395-E52F-467C-8E3D-C4579291692E");
    private static readonly Guid IID_IAudioEndpointVolume  = new("5CDF2C82-841E-4546-9722-0CF74078229A");

    // {B3F8FA53-0004-438E-9003-51A46E139BFC},2 = instance path (contains VID/PID)
    // {B3F8FA53-0004-438E-9003-51A46E139BFC},6 = product name (e.g. "ACU-624D 0J0619698")
    private static readonly Guid   PKEY_InstanceId_FMTID = new("B3F8FA53-0004-438E-9003-51A46E139BFC");
    private const           uint   PKEY_InstanceId_PID   = 2;
    private const           uint   PKEY_ProductName_PID  = 6;
    private const           ushort VT_LPWSTR             = 31;
    private const           uint   STGM_READ             = 0;

    // ── COM interfaces ────────────────────────────────────────────────────
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

    [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPropertyStore
    {
        [PreserveSig] int GetCount(out uint cProps);
        [PreserveSig] int GetAt(uint iProp, out PROPERTYKEY pkey);
        [PreserveSig] int GetValue(ref PROPERTYKEY key, out PROPVARIANT pv);
        [PreserveSig] int SetValue(ref PROPERTYKEY key, ref PROPVARIANT propvar);
        [PreserveSig] int Commit();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROPERTYKEY
    {
        public Guid fmtid;
        public uint pid;
    }

    // Minimal PROPVARIANT — only handles VT_LPWSTR (type 31) strings.
    // x64 Windows: sizeof(PROPVARIANT) = 24 (8-byte header + 16-byte union).
    // Use Size = 32 for safety so COM never overflows the buffer.
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    private struct PROPVARIANT
    {
        [FieldOffset(0)] public ushort vt;
        [FieldOffset(8)] public IntPtr pszVal;  // pointer for VT_LPWSTR

        public string? AsString()
        {
            if (vt != VT_LPWSTR || pszVal == IntPtr.Zero) return null;
            return Marshal.PtrToStringUni(pszVal);
        }

        // Zero out the struct. Intentionally does not call PropVariantClear /
        // CoTaskMemFree to avoid crashes when the pointer origin is unknown.
        // Acceptable tiny leak for a device-enumeration-time call.
        public void Clear() { vt = 0; pszVal = IntPtr.Zero; }
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
    }

    // ── State ─────────────────────────────────────────────────────────────
    private IAudioEndpointVolume? _renderVol;
    private IAudioEndpointVolume? _captureVol;
    private readonly string       _vidPidFilter;

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
            _renderVol  = FindEndpointVolume(enumerator, 0 /*eRender*/);
            _captureVol = FindEndpointVolume(enumerator, 1 /*eCapture*/);
            Marshal.ReleaseComObject(enumerator);
        }
        catch { }
    }

    private IAudioEndpointVolume? FindEndpointVolume(IMMDeviceEnumerator enumerator, int dataFlow)
    {
        if (enumerator.EnumAudioEndpoints(dataFlow, 1 /*ACTIVE*/, out var collection) != 0) return null;
        collection.GetCount(out uint count);

        for (uint i = 0; i < count; i++)
        {
            if (collection.Item(i, out var device) != 0) continue;

            if (DeviceMatchesVidPid(device))
            {
                Guid iid = IID_IAudioEndpointVolume;
                if (device.Activate(ref iid, 0x17, IntPtr.Zero, out IntPtr volPtr) == 0)
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

    private bool DeviceMatchesVidPid(IMMDevice device)
    {
        if (device.OpenPropertyStore(STGM_READ, out IntPtr storePtr) != 0) return false;
        var store = (IPropertyStore)Marshal.GetObjectForIUnknown(storePtr);
        Marshal.Release(storePtr);

        // Match by instance path (contains VID/PID) or product name (contains "ACU-624")
        var key = new PROPERTYKEY { fmtid = PKEY_InstanceId_FMTID, pid = PKEY_InstanceId_PID };
        store.GetValue(ref key, out PROPVARIANT pv);
        string? instanceId = pv.AsString(); pv.Clear();

        var key2 = new PROPERTYKEY { fmtid = PKEY_InstanceId_FMTID, pid = PKEY_ProductName_PID };
        store.GetValue(ref key2, out PROPVARIANT pv2);
        string? productName = pv2.AsString(); pv2.Clear();

        Marshal.ReleaseComObject(store);

        return (instanceId  != null && instanceId.ToUpperInvariant().Contains(_vidPidFilter)) ||
               (productName != null && productName.ToUpperInvariant().Contains("ACU-624"));
    }

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
