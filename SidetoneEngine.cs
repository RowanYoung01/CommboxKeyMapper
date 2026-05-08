using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace ACU624KeyMapper;

/// <summary>
/// Two-path sidetone for the ACU-624D.
///
/// Hardware path: walks the Windows WASAPI device topology starting from the
/// capture endpoint, finds the IAudioVolumeLevel and IAudioMute nodes that
/// represent the hardware sidetone mix, and controls them directly — exactly
/// as VCSPosition.exe does it on Windows 7/10.
///
/// Software path: captures from the ACU-624D microphone endpoint and plays
/// it back through the headset output at ~50 ms latency.
/// </summary>
public sealed class SidetoneEngine : IDisposable
{
    // ── COM GUIDs ─────────────────────────────────────────────────────────
    private static readonly Guid IID_IDeviceTopology      = new("2A07407E-6497-4A18-9787-32F79BD0D98F");
    private static readonly Guid IID_IAudioVolumeLevel     = new("7FB7B48F-531D-44A2-BCB3-5AD5A134B3DC");
    private static readonly Guid IID_IAudioMute            = new("DF45AEEA-B74A-4B6B-AFAD-2366B6AA012E");
    private static readonly Guid IID_IAudioEndpointVolume  = new("5CDF2C82-841E-4546-9722-0CF74078229A");
    // {B3F8FA53-0004-438E-9003-51A46E139BFC},2  — instance path, e.g. "{1}.USB\VID_0000&PID_3200&MI_00\..."
    // {B3F8FA53-0004-438E-9003-51A46E139BFC},6  — product name, e.g. "ACU-624D 0J0619698"
    // (These are the actual keys present in the WASAPI endpoint property store on Windows 11)
    private static readonly Guid PKEY_Device_InstanceId_FMTID = new("B3F8FA53-0004-438E-9003-51A46E139BFC");
    private const uint PKEY_Device_InstanceId_PID = 2;
    private const uint PKEY_Device_ProductName_PID = 6;

    // ── DeviceTopology COM interfaces ─────────────────────────────────────
    [ComImport, Guid("2A07407E-6497-4A18-9787-32F79BD0D98F"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IDeviceTopology
    {
        [PreserveSig] int GetConnectorCount(out uint pCount);
        [PreserveSig] int GetConnector(uint nIndex, [MarshalAs(UnmanagedType.Interface)] out IConnector ppConnector);
        [PreserveSig] int GetSubunitCount(out uint pCount);
        [PreserveSig] int GetSubunit(uint nIndex, out IntPtr ppSubunit);
        [PreserveSig] int GetPartById(uint nId, [MarshalAs(UnmanagedType.Interface)] out IPart ppPart);
        [PreserveSig] int GetDeviceId([MarshalAs(UnmanagedType.LPWStr)] out string ppwstrDeviceId);
        [PreserveSig] int GetSignalPath(IntPtr pFrom, IntPtr pTo, [MarshalAs(UnmanagedType.Bool)] bool bReject, out IntPtr ppParts);
    }

    [ComImport, Guid("9C2C4058-23F5-41DE-877A-DF3AF236A09E"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IConnector
    {
        [PreserveSig] int GetType(out int pType);
        [PreserveSig] int GetDataFlow(out int pFlow);
        [PreserveSig] int ConnectTo([MarshalAs(UnmanagedType.Interface)] IConnector pConnectTo);
        [PreserveSig] int Disconnect();
        [PreserveSig] int IsConnected([MarshalAs(UnmanagedType.Bool)] out bool pbConnected);
        [PreserveSig] int GetConnectedTo([MarshalAs(UnmanagedType.Interface)] out IConnector ppConTo);
        [PreserveSig] int GetConnectorIdConnectedTo([MarshalAs(UnmanagedType.LPWStr)] out string ppwstrDeviceId);
        [PreserveSig] int GetDeviceIdConnectedTo([MarshalAs(UnmanagedType.LPWStr)] out string ppwstrDeviceId);
    }

    [ComImport, Guid("AE2DE0E4-5BCA-4F2D-AA46-5D13F8FDB3A9"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPart
    {
        [PreserveSig] int GetName([MarshalAs(UnmanagedType.LPWStr)] out string ppwstrName);
        [PreserveSig] int GetLocalId(out uint pnId);
        [PreserveSig] int GetGlobalId([MarshalAs(UnmanagedType.LPWStr)] out string ppwstrGlobalId);
        [PreserveSig] int GetPartType(out int pPartType);
        [PreserveSig] int GetSubType(out Guid pSubType);
        [PreserveSig] int GetControlInterfaceCount(out uint pCount);
        [PreserveSig] int GetControlInterface(uint nIndex, out IntPtr ppInterfaceDesc);
        [PreserveSig] int EnumPartsIncoming([MarshalAs(UnmanagedType.Interface)] out IPartsList ppParts);
        [PreserveSig] int EnumPartsOutgoing([MarshalAs(UnmanagedType.Interface)] out IPartsList ppParts);
        [PreserveSig] int GetTopologyObject([MarshalAs(UnmanagedType.Interface)] out IDeviceTopology ppIDeviceTopology);
        [PreserveSig] int Activate(uint dwClsContext, ref Guid refiid, out IntPtr ppvObject);
    }

    [ComImport, Guid("6DAA848C-5EB0-45CC-AEA5-998A2CDA1FFB"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPartsList
    {
        [PreserveSig] int GetCount(out uint pCount);
        [PreserveSig] int GetPart(uint nIndex, [MarshalAs(UnmanagedType.Interface)] out IPart ppPart);
    }

    [ComImport, Guid("7FB7B48F-531D-44A2-BCB3-5AD5A134B3DC"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioVolumeLevel
    {
        [PreserveSig] int GetChannelCount(out uint pcChannels);
        [PreserveSig] int GetLevelRange(uint nChannel, out float pfMinLevelDB, out float pfMaxLevelDB, out float pfStepping);
        [PreserveSig] int GetLevel(uint nChannel, out float pfLevelDB);
        [PreserveSig] int SetLevel(uint nChannel, float fLevelDB, ref Guid pguidEventContext);
        [PreserveSig] int SetLevelUniform(float fLevelDB, ref Guid pguidEventContext);
        [PreserveSig] int SetLevelAllChannels([MarshalAs(UnmanagedType.LPArray)] float[] aLevelsDB, uint cChannels, ref Guid pguidEventContext);
    }

    [ComImport, Guid("DF45AEEA-B74A-4B6B-AFAD-2366B6AA012E"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioMute
    {
        [PreserveSig] int SetMute([MarshalAs(UnmanagedType.Bool)] bool bMuted, ref Guid pguidEventContext);
        [PreserveSig] int GetMute([MarshalAs(UnmanagedType.Bool)] out bool pbMuted);
    }

    // ── Raw IMMDevice / Enumerator (no NAudio — avoids .NET 8 InvalidCastException) ──
    private static readonly Guid CLSID_MMDeviceEnumerator = new("BCDE0395-E52F-467C-8E3D-C4579291692E");

    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IRawMMDeviceEnumerator
    {
        [PreserveSig] int EnumAudioEndpoints(int dataFlow, int stateMask,
            [MarshalAs(UnmanagedType.Interface)] out IRawMMDeviceCollection ppDevices);
        [PreserveSig] int GetDefaultAudioEndpoint(int dataFlow, int role,
            [MarshalAs(UnmanagedType.Interface)] out IRawMMDevice ppEndpoint);
        [PreserveSig] int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string pwstrId,
            [MarshalAs(UnmanagedType.Interface)] out IRawMMDevice ppDevice);
        [PreserveSig] int RegisterEndpointNotificationCallback(IntPtr pNotify);
        [PreserveSig] int UnregisterEndpointNotificationCallback(IntPtr pNotify);
    }

    [ComImport, Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IRawMMDeviceCollection
    {
        [PreserveSig] int GetCount(out uint pcDevices);
        [PreserveSig] int Item(uint nDevice,
            [MarshalAs(UnmanagedType.Interface)] out IRawMMDevice ppDevice);
    }

    [ComImport, Guid("5CDF2C82-841E-4546-9722-0CF74078229A"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IRawAudioEndpointVolume
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

    [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IRawMMDevice
    {
        [PreserveSig] int Activate(ref Guid iid, uint dwClsCtx, IntPtr pActivationParams, out IntPtr ppInterface);
        [PreserveSig] int OpenPropertyStore(uint stgmAccess, out IntPtr ppProperties);
        [PreserveSig] int GetId([MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);
        [PreserveSig] int GetState(out uint pdwState);
    }

    // Minimal PROPVARIANT for VT_LPWSTR (same layout as CoreAudioHelper)
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    private struct PROPVARIANT_ST
    {
        [FieldOffset(0)] public ushort vt;
        [FieldOffset(8)] public IntPtr pszVal;
        public string? AsString() => (vt == 31 && pszVal != IntPtr.Zero) ? Marshal.PtrToStringUni(pszVal) : null;
        public void Clear() { vt = 0; pszVal = IntPtr.Zero; }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROPERTYKEY_ST { public Guid fmtid; public uint pid; }

    [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IRawPropertyStore
    {
        [PreserveSig] int GetCount(out uint cProps);
        [PreserveSig] int GetAt(uint iProp, out PROPERTYKEY_ST pkey);
        [PreserveSig] int GetValue(ref PROPERTYKEY_ST key, out PROPVARIANT_ST pv);
        [PreserveSig] int SetValue(ref PROPERTYKEY_ST key, ref PROPVARIANT_ST propvar);
        [PreserveSig] int Commit();
    }

    /// <summary>
    /// Max out the WASAPI endpoint master volume (scalar 1.0) — same as VCSPosition does
    /// when initializing WASAPISource/WASAPISink, to ensure Windows doesn't bottle-neck audio.
    /// </summary>
    private static void MaxOutEndpointVolume(IRawMMDevice device)
    {
        try
        {
            var iid = IID_IAudioEndpointVolume;
            if (device.Activate(ref iid, 0x17, IntPtr.Zero, out IntPtr ptr) != 0) return;
            var vol = (IRawAudioEndpointVolume)Marshal.GetObjectForIUnknown(ptr);
            Marshal.Release(ptr);
            var ctx = Guid.Empty;
            vol.SetMasterVolumeLevelScalar(1.0f, ref ctx);
            vol.SetMute(false, ref ctx);
        }
        catch { }
    }

    /// <summary>
    /// Walk the entire device topology and set every IAudioVolumeLevel node to its maximum dB.
    /// Called at connect time to repair any previously-lowered topology volume nodes.
    /// </summary>
    private static void MaxOutAllTopologyVolumeNodes(IRawMMDevice device)
    {
        try
        {
            var topoGuid = IID_IDeviceTopology;
            if (device.Activate(ref topoGuid, 0x17, IntPtr.Zero, out IntPtr topoPtr) != 0) return;
            var topo = (IDeviceTopology)Marshal.GetObjectForIUnknown(topoPtr);
            Marshal.Release(topoPtr);

            topo.GetConnectorCount(out uint connCount);
            for (uint c = 0; c < connCount; c++)
            {
                if (topo.GetConnector(c, out IConnector conn) != 0) continue;
                if (conn.GetConnectedTo(out IConnector connHw) != 0) continue;
                var startPart = connHw as IPart;
                if (startPart == null) continue;

                var visited = new HashSet<uint>();
                var queue = new Queue<IPart>();
                queue.Enqueue(startPart);
                while (queue.Count > 0)
                {
                    var part = queue.Dequeue();
                    if (part.GetLocalId(out uint id) != 0) continue;
                    if (!visited.Add(id)) continue;

                    var volGuid = IID_IAudioVolumeLevel;
                    if (part.Activate(0x17, ref volGuid, out IntPtr volPtr) == 0)
                    {
                        var vol = (IAudioVolumeLevel)Marshal.GetObjectForIUnknown(volPtr);
                        Marshal.Release(volPtr);
                        try
                        {
                            vol.GetLevelRange(0, out _, out float maxDb, out _);
                            var ctx = Guid.Empty;
                            vol.SetLevelUniform(maxDb, ref ctx);
                        }
                        catch { }
                    }

                    if (part.EnumPartsOutgoing(out IPartsList outList) == 0)
                    {
                        outList.GetCount(out uint cnt);
                        for (uint p = 0; p < cnt; p++)
                            if (outList.GetPart(p, out IPart next) == 0) queue.Enqueue(next);
                    }
                    if (part.EnumPartsIncoming(out IPartsList inList) == 0)
                    {
                        inList.GetCount(out uint cnt);
                        for (uint p = 0; p < cnt; p++)
                            if (inList.GetPart(p, out IPart next) == 0) queue.Enqueue(next);
                    }
                }
            }
        }
        catch { }
    }

    /// <summary>
    /// Restore all WASAPI endpoint and topology volumes to maximum for this device.
    /// Call once after connecting to repair any previously-lowered volume nodes.
    /// </summary>
    public void RestoreAllVolumes()
    {
        var render  = FindRawDevice(0 /*Render*/);
        var capture = FindRawDevice(1 /*Capture*/);
        if (render  != null) { MaxOutEndpointVolume(render);  MaxOutAllTopologyVolumeNodes(render);  }
        if (capture != null) { MaxOutEndpointVolume(capture); MaxOutAllTopologyVolumeNodes(capture); }
    }

    /// <summary>
    /// Find the ACU-624D audio endpoint using raw COM (no NAudio), matching VID/PID
    /// via PKEY_Device_InstanceId in the property store.
    /// dataFlow: 0=Render, 1=Capture
    /// Returns null if not found.
    /// </summary>

    private string? ReadStringProp(IRawPropertyStore store, Guid fmtid, uint pid)
    {
        var pkey = new PROPERTYKEY_ST { fmtid = fmtid, pid = pid };
        if (store.GetValue(ref pkey, out PROPVARIANT_ST pv) != 0) return null;
        var s = pv.AsString();
        pv.Clear();
        return s;
    }

    private IRawMMDevice? FindRawDevice(int dataFlow)
    {
        DiagnosticEndpoints = null;
        try
        {
            var enumType = Type.GetTypeFromCLSID(CLSID_MMDeviceEnumerator)!;
            var enumerator = (IRawMMDeviceEnumerator)Activator.CreateInstance(enumType)!;
            var filter = VidPidFilter;

            IRawMMDevice? ScanActive()
            {
                if (enumerator.EnumAudioEndpoints(dataFlow, 0x1 /*ACTIVE*/, out var col) != 0) return null;
                col.GetCount(out uint count);
                for (uint i = 0; i < count; i++)
                {
                    if (col.Item(i, out var device) != 0) continue;
                    if (device.OpenPropertyStore(0, out IntPtr storePtr) != 0) continue;
                    var store = (IRawPropertyStore)Marshal.GetObjectForIUnknown(storePtr);
                    Marshal.Release(storePtr);

                    // Match by instance path (contains VID/PID) or product name (contains "ACU-624")
                    string? instanceId   = ReadStringProp(store, PKEY_Device_InstanceId_FMTID, PKEY_Device_InstanceId_PID);
                    string? productName  = ReadStringProp(store, PKEY_Device_InstanceId_FMTID, PKEY_Device_ProductName_PID);

                    if ((instanceId  != null && instanceId.ToUpperInvariant().Contains(filter)) ||
                        (productName != null && productName.ToUpperInvariant().Contains("ACU-624")))
                        return device;
                }
                return null;
            }

            // First try active-only (normal case)
            var result = ScanActive();
            if (result != null) return result;

            // Not found as ACTIVE — dump all endpoints with id + friendly name + state for diagnosis
            if (enumerator.EnumAudioEndpoints(dataFlow, 0xF /*ALL*/, out var allCol) == 0)
            {
                allCol.GetCount(out uint allCount);
                var seen = new List<string>();
                for (uint i = 0; i < allCount; i++)
                {
                    if (allCol.Item(i, out var device) != 0) continue;
                    device.GetState(out uint state);
                    string stateStr = state switch { 1=>"active", 2=>"disabled", 4=>"not_present", 8=>"unplugged", _=>$"0x{state:X}" };
                    device.GetId(out string? epId);

                    if (device.OpenPropertyStore(0, out IntPtr storePtr) != 0) { seen.Add($"id={epId}[{stateStr}](no-store)"); continue; }
                    var store = (IRawPropertyStore)Marshal.GetObjectForIUnknown(storePtr);
                    Marshal.Release(storePtr);

                    var pkey = new PROPERTYKEY_ST { fmtid = PKEY_Device_InstanceId_FMTID, pid = PKEY_Device_InstanceId_PID };
                    store.GetValue(ref pkey, out PROPVARIANT_ST pv);
                    string? instanceId = pv.AsString();
                    ushort vt = pv.vt;
                    pv.Clear();

                    string? productName = ReadStringProp(store, PKEY_Device_InstanceId_FMTID, PKEY_Device_ProductName_PID);
                    seen.Add($"name={productName ?? "?"} iid={instanceId ?? $"(vt={vt})"} [{stateStr}]");
                }
                DiagnosticEndpoints = seen.Count > 0 ? string.Join(" | ", seen) : "(no endpoints in any state)";
            }
            else
            {
                DiagnosticEndpoints = "(EnumAudioEndpoints failed)";
            }
        }
        catch (Exception ex) { DiagnosticEndpoints = $"Exception: {ex.Message}"; }
        return null;
    }

    /// <summary>Set when FindRawDevice fails — lists all instance IDs seen during enumeration.</summary>
    public string? DiagnosticEndpoints { get; private set; }

    // ── State ─────────────────────────────────────────────────────────────
    private readonly ushort _vid, _pid;
    private WasapiCapture?         _capture;
    private WasapiOut?             _player;
    private BufferedWaveProvider?  _buffer;
    private VolumeSampleProvider?  _volumeProvider;
    private bool                   _softwareRunning;

    public bool    IsRunning      => _softwareRunning;
    public float   SoftwareVolume { get; private set; } = 0.5f;
    public string? LastError      { get; private set; }

    public SidetoneEngine(ushort vid = 0x0000, ushort pid = 0x3200)
    {
        _vid = vid;
        _pid = pid;
    }

    // ── Device discovery (shared by hardware and software paths) ──────────
    private string VidPidFilter => $"VID_{_vid:X4}&PID_{_pid:X4}".ToUpperInvariant();

    private MMDevice? FindDevice(DataFlow flow)
    {
        using var enumerator = new MMDeviceEnumerator();
        var collection = enumerator.EnumerateAudioEndPoints(flow, DeviceState.Active);
        var filter = VidPidFilter;

        // First pass: check endpoint ID (works on some Windows builds)
        foreach (var dev in collection)
            if (dev.ID.ToUpperInvariant().Contains(filter))
                return dev;

        // Second pass: check device instance ID from property store
        var instanceKey = new PropertyKey(PKEY_Device_InstanceId_FMTID, (int)PKEY_Device_InstanceId_PID);
        collection = enumerator.EnumerateAudioEndPoints(flow, DeviceState.Active);
        foreach (var dev in collection)
        {
            try
            {
                var val = dev.Properties[instanceKey]?.Value?.ToString() ?? "";
                if (val.ToUpperInvariant().Contains(filter))
                    return dev;
            }
            catch { /* property unavailable */ }
        }

        return null;
    }

    // ── Hardware path: HID Feature Report (ACU-624D Audio Box) ───────────
    /// <summary>
    /// Sends hardware sidetone control via Feature Report ID 0x04 — the path used
    /// using Output Report ID 0x05 (vendor page 0xFF00, usages 0x20-0x27).
    ///
    /// Report layout confirmed from HID descriptor (Out:9 = 1 ID + 8 data bytes):
    ///   Byte 0:   Report ID = 0x05
    ///   Bytes 1-8: 8 sidetone mix levels (0-64 each), one per usage 0x20-0x27.
    ///              These are the hardware sidetone mix gains for each channel pair.
    ///              0 = silent, 64 = full level.
    ///
    /// VCS Volume.ini uses 70% for all sidetone volumes → 70/100 × 64 ≈ 45.
    /// </summary>
    public static bool TryHidSidetone(Microsoft.Win32.SafeHandles.SafeFileHandle handle,
                                      bool enable, float volumePct)
    {
        if (handle == null || handle.IsInvalid) return false;

        // Report ID 5: vendor-page 0xFF00, 8 attenuation bytes (0-64 each).
        // 0 = no attenuation (full sidetone), 64 = fully muted.
        // VCS Volume.ini sidetone = 70 → attenuation = (100-70)/100 × 64 ≈ 19.
        byte atten = enable
            ? (byte)Math.Clamp((int)((100f - volumePct) / 100f * 64), 0, 64)
            : (byte)64;
        var report = new byte[9];
        report[0] = 0x05;
        for (int i = 1; i <= 8; i++) report[i] = atten;

        uint written = 0;
        return HidHelper.WriteFile(handle, report, 9, ref written, IntPtr.Zero) && written == 9;
    }

    // ── Hardware path: WASAPI device topology ─────────────────────────────
    /// <summary>
    /// Finds and controls the hardware sidetone mixer node inside the ACU-624D
    /// using the Windows WASAPI device topology API (same approach as VCSPosition.exe).
    /// Returns true if the controls were found and set successfully.
    /// </summary>
    public bool TrySetHardwareSidetone(bool enable, float volumePct)
    {
        // Use raw COM to find the device — avoids NAudio MMDeviceEnumerator .NET 8 crash
        var captureDevice = FindRawDevice(1 /*Capture*/);
        if (captureDevice == null)
        {
            LastError = $"HW sidetone: capture device not found ({VidPidFilter}). Endpoints seen: {DiagnosticEndpoints ?? "(none)"}";
            return false;
        }

        // Max out endpoint volumes on both render and capture — same as VCSPosition does on init.
        // This restores any previously-lowered endpoint scalar so computer audio plays at full volume.
        if (enable)
        {
            MaxOutEndpointVolume(captureDevice);
            var renderDevice = FindRawDevice(0 /*Render*/);
            if (renderDevice != null) MaxOutEndpointVolume(renderDevice);
        }

        try
        {
            // Activate IDeviceTopology directly on the raw COM device
            var topoGuid = IID_IDeviceTopology;
            if (captureDevice.Activate(ref topoGuid, 0x17 /*CLSCTX_ALL*/, IntPtr.Zero, out IntPtr topoPtr) != 0)
                return false;

            var topo = (IDeviceTopology)Marshal.GetObjectForIUnknown(topoPtr);
            Marshal.Release(topoPtr);

            // Walk every connector in the topology
            topo.GetConnectorCount(out uint connCount);
            for (uint c = 0; c < connCount; c++)
            {
                if (topo.GetConnector(c, out IConnector conn) != 0) continue;

                // Step across to the device-hardware side
                if (conn.GetConnectedTo(out IConnector connHw) != 0) continue;

                // QI for IPart
                var startPart = connHw as IPart;
                if (startPart == null) continue;

                if (SearchTopologyForSidetone(startPart, enable, volumePct))
                    return true;
            }
        }
        catch (Exception ex) { LastError = $"HW sidetone topology: {ex.GetType().Name}: {ex.Message}"; }

        return false;
    }

    private static bool SearchTopologyForSidetone(IPart startPart, bool enable, float volumePct)
    {
        IAudioVolumeLevel? volCtrl = null;
        IAudioMute?        muteCtrl = null;

        var visited = new HashSet<uint>();
        var queue   = new Queue<IPart>();
        queue.Enqueue(startPart);

        while (queue.Count > 0)
        {
            var part = queue.Dequeue();
            if (part.GetLocalId(out uint id) != 0) continue;
            if (!visited.Add(id)) continue;

            // Try to get volume control from this part
            if (volCtrl == null)
            {
                var volGuid = IID_IAudioVolumeLevel;
                if (part.Activate(0x17, ref volGuid, out IntPtr volPtr) == 0)
                {
                    volCtrl = (IAudioVolumeLevel)Marshal.GetObjectForIUnknown(volPtr);
                    Marshal.Release(volPtr);
                }
            }

            // Try to get mute control from this part
            if (muteCtrl == null)
            {
                var muteGuid = IID_IAudioMute;
                if (part.Activate(0x17, ref muteGuid, out IntPtr mutePtr) == 0)
                {
                    muteCtrl = (IAudioMute)Marshal.GetObjectForIUnknown(mutePtr);
                    Marshal.Release(mutePtr);
                }
            }

            // Enqueue outgoing (towards hardware source)
            if (part.EnumPartsOutgoing(out IPartsList outList) == 0)
            {
                outList.GetCount(out uint outCount);
                for (uint p = 0; p < outCount; p++)
                    if (outList.GetPart(p, out IPart next) == 0)
                        queue.Enqueue(next);
            }

            // Also enqueue incoming (towards software endpoint)
            if (part.EnumPartsIncoming(out IPartsList inList) == 0)
            {
                inList.GetCount(out uint inCount);
                for (uint p = 0; p < inCount; p++)
                    if (inList.GetPart(p, out IPart next) == 0)
                        queue.Enqueue(next);
            }
        }

        if (muteCtrl == null) return false;

        var ctx = Guid.Empty;
        muteCtrl.SetMute(!enable, ref ctx);

        // If enabling sidetone, restore the volume node to maximum — previous code may have
        // left it at a low dB level which persists in Windows and drops computer audio.
        if (enable && volCtrl != null)
        {
            volCtrl.GetLevelRange(0, out _, out float maxDb, out _);
            volCtrl.SetLevelUniform(maxDb, ref ctx);
        }

        return true;
    }

    // ── Software path (WASAPI loopback) ───────────────────────────────────
    public bool StartSoftware(float volume = 0.5f)
    {
        LastError = null;
        StopSoftware();

        try
        {
            // Try to find the specific ACU-624D endpoints.
            // NAudio's MMDeviceEnumerator can fail on .NET 8 (InvalidCastException on
            // MMDeviceEnumeratorComObject). Fall back to system default devices if so.
            MMDevice? micDevice = null, spkDevice = null;
            try
            {
                micDevice = FindDevice(DataFlow.Capture);
                spkDevice = FindDevice(DataFlow.Render);
                if (micDevice == null)
                    LastError = $"Capture device not found (VID_{_vid:X4}&PID_{_pid:X4}); using default";
                if (spkDevice == null)
                    LastError = $"Render device not found (VID_{_vid:X4}&PID_{_pid:X4}); using default";
            }
            catch (Exception ex)
            {
                LastError = $"FindDevice {ex.GetType().Name}: {ex.Message}; using default devices";
            }

            if (micDevice != null)
                _capture = new WasapiCapture(micDevice, true, 50);
            else
                _capture = new WasapiCapture();          // system default mic

            if (spkDevice != null)
                _player = new WasapiOut(spkDevice, AudioClientShareMode.Shared, true, 50);
            else
                _player = new WasapiOut(AudioClientShareMode.Shared, true, 50); // system default output

            var waveFormat = _capture.WaveFormat;
            _buffer = new BufferedWaveProvider(waveFormat) { DiscardOnBufferOverflow = true };

            // Convert capture format to IEEE float samples (required by VolumeSampleProvider).
            // WaveFormatEncoding.Extensible wraps many sub-formats; check BitsPerSample/SubType.
            ISampleProvider sampleSrc;
            if (waveFormat.Encoding == WaveFormatEncoding.IeeeFloat ||
                (waveFormat.Encoding == WaveFormatEncoding.Extensible && waveFormat.BitsPerSample == 32))
                sampleSrc = _buffer.ToSampleProvider();
            else
                sampleSrc = new Wave16ToFloatProvider(_buffer).ToSampleProvider();

            _volumeProvider = new VolumeSampleProvider(sampleSrc) { Volume = volume };
            SoftwareVolume = volume;

            _player.Init(_volumeProvider);
            _capture.DataAvailable += OnCaptureData;
            _player.Play();
            _capture.StartRecording();
            _softwareRunning = true;
            return true;
        }
        catch (Exception ex)
        {
            LastError = $"{ex.GetType().Name}: {ex.Message}";
            StopSoftware();
            return false;
        }
    }

    private void OnCaptureData(object? sender, WaveInEventArgs e)
        => _buffer?.AddSamples(e.Buffer, 0, e.BytesRecorded);

    public void SetSoftwareVolume(float volume)
    {
        SoftwareVolume = Math.Clamp(volume, 0f, 1f);
        if (_volumeProvider != null)
            _volumeProvider.Volume = SoftwareVolume;
    }

    public void StopSoftware()
    {
        if (_capture != null)
        {
            try { _capture.StopRecording(); } catch { }
            _capture.DataAvailable -= OnCaptureData;
            _capture.Dispose();
            _capture = null;
        }
        if (_player != null)
        {
            try { _player.Stop(); } catch { }
            _player.Dispose();
            _player = null;
        }
        _buffer         = null;
        _volumeProvider = null;
        _softwareRunning = false;
    }

    public void Dispose() => StopSoftware();
}
