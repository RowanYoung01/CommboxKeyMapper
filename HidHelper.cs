using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace ACU624KeyMapper;

public static class HidHelper
{
    // ── Win32 constants ───────────────────────────────────────────────────
    private const uint DIGCF_PRESENT         = 0x00000002;
    private const uint DIGCF_DEVICEINTERFACE = 0x00000010;
    private const uint GENERIC_READ          = 0x80000000;
    private const uint GENERIC_WRITE         = 0x40000000;
    private const uint FILE_SHARE_READ       = 0x00000001;
    private const uint FILE_SHARE_WRITE      = 0x00000002;
    private const uint OPEN_EXISTING         = 3;

    // ── Win32 structs ─────────────────────────────────────────────────────
    [StructLayout(LayoutKind.Sequential)]
    private struct SP_DEVICE_INTERFACE_DATA
    {
        public int  cbSize;
        public Guid InterfaceClassGuid;
        public int  Flags;
        public IntPtr Reserved;
    }

    // The DevicePath field needs a large buffer; we allocate via Marshal manually.
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct SP_DEVICE_INTERFACE_DETAIL_DATA
    {
        public int cbSize;
        // DevicePath follows immediately after cbSize in memory.
        // We read it separately via Marshal.
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HIDD_ATTRIBUTES
    {
        public int    Size;
        public ushort VendorID;
        public ushort ProductID;
        public ushort VersionNumber;
    }

    // ── P/Invoke ──────────────────────────────────────────────────────────
    [DllImport("hid.dll")]
    private static extern void HidD_GetHidGuid(out Guid HidGuid);

    [DllImport("hid.dll")]
    public static extern bool HidD_GetAttributes(SafeFileHandle device, ref HIDD_ATTRIBUTES attributes);

    [DllImport("hid.dll")]
    public static extern bool HidD_SetFeature(SafeFileHandle device, byte[] reportBuffer, uint bufferLength);

    [DllImport("hid.dll")]
    public static extern bool HidD_GetFeature(SafeFileHandle device, byte[] reportBuffer, uint bufferLength);

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern IntPtr SetupDiGetClassDevs(ref Guid ClassGuid, IntPtr Enumerator,
        IntPtr hwndParent, uint Flags);

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiEnumDeviceInterfaces(IntPtr DeviceInfoSet, IntPtr DeviceInfoData,
        ref Guid InterfaceClassGuid, uint MemberIndex, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);

    [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr DeviceInfoSet,
        ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData,
        IntPtr DeviceInterfaceDetailData, uint DeviceInterfaceDetailDataSize,
        out uint RequiredSize, IntPtr DeviceInfoData);

    [DllImport("setupapi.dll")]
    private static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern SafeFileHandle CreateFile(string lpFileName, uint dwDesiredAccess,
        uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition,
        uint dwFlagsAndAttributes, IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool ReadFile(SafeFileHandle hFile, byte[] lpBuffer,
        uint nNumberOfBytesToRead, ref uint lpNumberOfBytesRead, IntPtr lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool WriteFile(SafeFileHandle hFile, byte[] lpBuffer,
        uint nNumberOfBytesToWrite, ref uint lpNumberOfBytesWritten, IntPtr lpOverlapped);

    [DllImport("hid.dll", CharSet = CharSet.Auto)]
    private static extern bool HidD_GetProductString(SafeFileHandle device,
        [MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder buffer, uint bufferLength);

    [DllImport("hid.dll")]
    private static extern bool HidD_GetPreparsedData(SafeFileHandle device, out IntPtr preparsedData);

    [DllImport("hid.dll")]
    private static extern bool HidD_FreePreparsedData(IntPtr preparsedData);

    [DllImport("hid.dll")]
    private static extern int HidP_GetCaps(IntPtr preparsedData, out HIDP_CAPS capabilities);

    [DllImport("hid.dll")]
    private static extern int HidP_GetValueCaps(HidReportType reportType, [Out] HIDP_VALUE_CAPS[] valueCaps,
        ref ushort valueCapsLength, IntPtr preparsedData);

    [DllImport("hid.dll")]
    private static extern int HidP_GetButtonCaps(HidReportType reportType, [Out] HIDP_BUTTON_CAPS[] buttonCaps,
        ref ushort buttonCapsLength, IntPtr preparsedData);

    private enum HidReportType { Input = 0, Output = 1, Feature = 2 }

    // HidP_* functions return this NTSTATUS on success (not 0)
    private const int HIDP_STATUS_SUCCESS = 0x00110000;

    [StructLayout(LayoutKind.Sequential)]
    private struct HIDP_CAPS
    {
        public ushort Usage; public ushort UsagePage;
        public ushort InputReportByteLength;
        public ushort OutputReportByteLength;
        public ushort FeatureReportByteLength;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)] public ushort[] Reserved;
        public ushort NumberLinkCollectionNodes;
        public ushort NumberInputButtonCaps;
        public ushort NumberInputValueCaps;
        public ushort NumberInputDataIndices;
        public ushort NumberOutputButtonCaps;
        public ushort NumberOutputValueCaps;
        public ushort NumberOutputDataIndices;
        public ushort NumberFeatureButtonCaps;
        public ushort NumberFeatureValueCaps;
        public ushort NumberFeatureDataIndices;
    }

    // Must match HIDP_VALUE_CAPS exactly (72 bytes on x64 with default packing).
    [StructLayout(LayoutKind.Sequential)]
    private struct HIDP_VALUE_CAPS
    {
        public ushort UsagePage; public byte ReportID; public byte IsAlias;
        public ushort BitField; public ushort LinkCollection;
        public ushort LinkUsage; public ushort LinkUsagePage;
        public byte IsRange; public byte IsStringRange; public byte IsDesignatorRange; public byte HasNull;
        public byte NullValue; public byte BitSize;
        public ushort ReportCount;
        // Reserved[5] — 5 × USHORT = 10 bytes (was missing, causing 18-byte underrun)
        private ushort _r0, _r1, _r2, _r3, _r4;
        // UnitsExp + Units — 8 bytes (was missing)
        public uint UnitsExp; public uint Units;
        // Logical / Physical range
        public int LogicalMin; public int LogicalMax;
        public int PhysicalMin; public int PhysicalMax;
        // Union: Range (UsageMin/Max) or NotRange (Usage/Reserved)
        public ushort UsageMin; public ushort UsageMax;
        public ushort StringMin; public ushort StringMax;
        public ushort DesignatorMin; public ushort DesignatorMax;
        public ushort DataIndexMin; public ushort DataIndexMax;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HIDP_BUTTON_CAPS
    {
        public ushort UsagePage; public byte ReportID; public byte IsAlias;
        public ushort BitField; public ushort LinkCollection;
        public ushort LinkUsage; public ushort LinkUsagePage;
        public byte IsRange; public byte IsStringRange; public byte IsDesignatorRange; public byte HasNull;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)] public byte[] Reserved;
        public ushort UsageMin; public ushort UsageMax;
        public ushort StringMin; public ushort StringMax;
        public ushort DesignatorMin; public ushort DesignatorMax;
        public ushort DataIndexMin; public ushort DataIndexMax;
    }

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the Output and Feature report byte lengths (including Report ID byte)
    /// as declared in the device's HID descriptor. WriteFile and HidD_SetFeature require
    /// buffers padded to exactly these lengths.
    /// </summary>
    public static (int outputLen, int featureLen) GetReportLengths(SafeFileHandle handle)
    {
        if (handle == null || handle.IsInvalid) return (0, 0);
        if (!HidD_GetPreparsedData(handle, out IntPtr preparsed)) return (0, 0);
        try
        {
            if (HidP_GetCaps(preparsed, out HIDP_CAPS caps) != HIDP_STATUS_SUCCESS) return (0, 0);
            return (caps.OutputReportByteLength, caps.FeatureReportByteLength);
        }
        finally { HidD_FreePreparsedData(preparsed); }
    }

    /// <summary>
    /// Returns a multi-line string describing all HID reports on the device:
    /// usage pages, report IDs, byte lengths, and per-report value/button usages.
    /// </summary>
    public static string DumpReportCaps(string devicePath)
    {
        var sb = new System.Text.StringBuilder();
        using var h = OpenDeviceHandle(devicePath);
        if (h.IsInvalid) return "Cannot open device";

        if (!HidD_GetPreparsedData(h, out IntPtr preparsed)) return "HidD_GetPreparsedData failed";
        try
        {
            if (HidP_GetCaps(preparsed, out HIDP_CAPS caps) != HIDP_STATUS_SUCCESS) return "HidP_GetCaps failed";

            sb.AppendLine($"Top-level Usage: Page=0x{caps.UsagePage:X4}  Usage=0x{caps.Usage:X4}");
            sb.AppendLine($"Report lengths — In:{caps.InputReportByteLength}  Out:{caps.OutputReportByteLength}  Feat:{caps.FeatureReportByteLength}");
            sb.AppendLine($"Counts — InBtn:{caps.NumberInputButtonCaps} InVal:{caps.NumberInputValueCaps} OutBtn:{caps.NumberOutputButtonCaps} OutVal:{caps.NumberOutputValueCaps} FeatBtn:{caps.NumberFeatureButtonCaps} FeatVal:{caps.NumberFeatureValueCaps}");

            foreach (HidReportType rtype in new[] { HidReportType.Input, HidReportType.Output, HidReportType.Feature })
            {
                ushort valCount = rtype switch {
                    HidReportType.Input   => caps.NumberInputValueCaps,
                    HidReportType.Output  => caps.NumberOutputValueCaps,
                    _                     => caps.NumberFeatureValueCaps };
                ushort btnCount = rtype switch {
                    HidReportType.Input   => caps.NumberInputButtonCaps,
                    HidReportType.Output  => caps.NumberOutputButtonCaps,
                    _                     => caps.NumberFeatureButtonCaps };

                if (valCount > 0)
                {
                    var vcaps = new HIDP_VALUE_CAPS[valCount];
                    if (HidP_GetValueCaps(rtype, vcaps, ref valCount, preparsed) == HIDP_STATUS_SUCCESS)
                        foreach (var vc in vcaps)
                            sb.AppendLine($"  {rtype} VALUE  RptID=0x{vc.ReportID:X2}  Page=0x{vc.UsagePage:X4}  Usage=0x{vc.UsageMin:X4}  Bits={vc.BitSize}  Log=[{vc.LogicalMin}..{vc.LogicalMax}]");
                }
                if (btnCount > 0)
                {
                    var bcaps = new HIDP_BUTTON_CAPS[btnCount];
                    if (HidP_GetButtonCaps(rtype, bcaps, ref btnCount, preparsed) == HIDP_STATUS_SUCCESS)
                        foreach (var bc in bcaps)
                            sb.AppendLine($"  {rtype} BUTTON RptID=0x{bc.ReportID:X2}  Page=0x{bc.UsagePage:X4}  Usage=0x{bc.UsageMin:X4}..0x{bc.UsageMax:X4}");
                }
            }
        }
        finally { HidD_FreePreparsedData(preparsed); }

        return sb.ToString();
    }

    /// <summary>Returns all HID device paths matching the given VID and PID.</summary>
    public static List<string> FindDevicePaths(ushort vid, ushort pid)
    {
        var results = new List<string>();
        HidD_GetHidGuid(out Guid hidGuid);

        IntPtr devInfoSet = SetupDiGetClassDevs(ref hidGuid, IntPtr.Zero, IntPtr.Zero,
            DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
        if (devInfoSet == IntPtr.Zero || devInfoSet == new IntPtr(-1)) return results;

        try
        {
            for (uint index = 0; ; index++)
            {
                var ifData = new SP_DEVICE_INTERFACE_DATA { cbSize = Marshal.SizeOf<SP_DEVICE_INTERFACE_DATA>() };
                if (!SetupDiEnumDeviceInterfaces(devInfoSet, IntPtr.Zero, ref hidGuid, index, ref ifData))
                    break;

                // First call: get required buffer size
                SetupDiGetDeviceInterfaceDetail(devInfoSet, ref ifData, IntPtr.Zero, 0, out uint reqSize, IntPtr.Zero);
                if (reqSize == 0) continue;

                // Second call: allocate buffer and fill
                IntPtr detailBuf = Marshal.AllocHGlobal((int)reqSize);
                try
                {
                    // cbSize of DETAIL_DATA struct: 5 on x86, 8 on x64
                    Marshal.WriteInt32(detailBuf, IntPtr.Size == 8 ? 8 : 5);
                    if (!SetupDiGetDeviceInterfaceDetail(devInfoSet, ref ifData, detailBuf, reqSize, out _, IntPtr.Zero))
                        continue;

                    // DevicePath starts 4 bytes after cbSize (at offset 4)
                    string path = Marshal.PtrToStringAuto(detailBuf + 4) ?? string.Empty;
                    if (string.IsNullOrEmpty(path)) continue;

                    // Open to check VID/PID
                    using var h = OpenDeviceHandle(path);
                    if (h.IsInvalid) continue;

                    var attrs = new HIDD_ATTRIBUTES { Size = Marshal.SizeOf<HIDD_ATTRIBUTES>() };
                    if (HidD_GetAttributes(h, ref attrs) && attrs.VendorID == vid && attrs.ProductID == pid)
                        results.Add(path);
                }
                finally
                {
                    Marshal.FreeHGlobal(detailBuf);
                }
            }
        }
        finally
        {
            SetupDiDestroyDeviceInfoList(devInfoSet);
        }

        return results;
    }

    public record DeviceInfo(string Model, string Serial, string Firmware, string Manufacturer);

    /// <summary>Opens a HID device and reads product/version strings.</summary>
    public static DeviceInfo? GetDeviceInfo(string path)
    {
        using var h = OpenDeviceHandle(path);
        if (h.IsInvalid) return null;

        var attrs = new HIDD_ATTRIBUTES { Size = Marshal.SizeOf<HIDD_ATTRIBUTES>() };
        if (!HidD_GetAttributes(h, ref attrs)) return null;

        var buf = new System.Text.StringBuilder(256);
        string product = HidD_GetProductString(h, buf, (uint)(buf.Capacity * 2)) ? buf.ToString().Trim() : "";

        // Product string is typically "ACU-624D 0J0619698" — model then serial
        string model = "ACU-624D", serial = "";
        int space = product.IndexOf(' ');
        if (space > 0) { model = product[..space]; serial = product[(space + 1)..]; }
        else if (product.Length > 0) model = product;

        // release_number is BCD: 0x0102 = version 1.02
        ushort ver = attrs.VersionNumber;
        string firmware = $"{(ver >> 8) & 0xFF}.{ver & 0xFF:D2}";

        return new DeviceInfo(model, serial, firmware, "Adacel Inc.");
    }

    /// <summary>Opens a HID device for reading (shared access so other apps can still use it).</summary>
    public static SafeFileHandle OpenDevice(string path) => OpenDeviceHandle(path);

    private static SafeFileHandle OpenDeviceHandle(string path)
    {
        // Try read+write first, fall back to read-only (sufficient for input-only devices)
        var h = CreateFile(path, GENERIC_READ | GENERIC_WRITE,
            FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
        if (h.IsInvalid)
            h = CreateFile(path, GENERIC_READ,
                FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
        return h;
    }
}

/// <summary>
/// Writes text to the ACU-624D's 2-line × 16-character physical display via HID output reports.
///
/// Report ID 2 — Cursor Position (1 data byte):
///   Upper nibble = row  (0 = top, 1 = bottom)
///   Lower nibble = column (0-15)
///
/// Report ID 3 — Character Data (4 bytes, ASCII 0x20-0x7E):
///   The device auto-advances the cursor after each 4-byte write.
/// </summary>
public static class DeviceDisplay
{
    private const byte ReportIdCursor = 0x02;
    private const byte ReportIdChars  = 0x03;

    // Cached from HID descriptor — WriteFile requires exactly this many bytes per report.
    private static int _outputLen;

    /// <summary>
    /// Reads and caches the output report byte length from the device descriptor.
    /// Must be called once after opening the device handle, before any Write calls.
    /// </summary>
    public static void Setup(SafeFileHandle handle)
    {
        var (outLen, _) = HidHelper.GetReportLengths(handle);
        _outputLen = outLen > 0 ? outLen : 0;
    }

    public static void Reset() => _outputLen = 0;

    private static bool SetCursor(SafeFileHandle handle, int row, int col)
    {
        int len    = Math.Max(_outputLen, 2);
        var report = new byte[len];
        report[0] = ReportIdCursor;
        report[1] = (byte)((row << 4) | (col & 0x0F));
        uint written = 0;
        return HidHelper.WriteFile(handle, report, (uint)report.Length, ref written, IntPtr.Zero)
               && written == report.Length;
    }

    private static bool WriteChunk(SafeFileHandle handle, ReadOnlySpan<char> chars4)
    {
        int len    = Math.Max(_outputLen, 5);
        var report = new byte[len];
        report[0] = ReportIdChars;
        for (int i = 0; i < 4; i++)
        {
            char c = i < chars4.Length ? chars4[i] : ' ';
            report[i + 1] = (c >= 0x20 && c <= 0x7E) ? (byte)c : (byte)0x20;
        }
        uint written = 0;
        return HidHelper.WriteFile(handle, report, (uint)report.Length, ref written, IntPtr.Zero)
               && written == report.Length;
    }

    public static bool WriteLine(SafeFileHandle handle, int row, string text)
    {
        if (handle == null || handle.IsInvalid) return false;
        string line = (text + new string(' ', 16))[..16];
        if (!SetCursor(handle, row, 0)) return false;
        for (int i = 0; i < 16; i += 4)
        {
            if (!WriteChunk(handle, line.AsSpan(i, 4))) return false;
        }
        return true;
    }

    public static bool Write2Lines(SafeFileHandle handle, string line1, string line2)
    {
        if (handle == null || handle.IsInvalid) return false;
        return WriteLine(handle, 0, line1) && WriteLine(handle, 1, line2);
    }

    public static bool Clear(SafeFileHandle handle) =>
        Write2Lines(handle, "                ", "                ");

    /// <summary>
    /// Sends the display-settings Feature Report (Report ID 0x01) to the ACU-624D.
    /// VCSPosition.exe configures: Backlit = 1 (on), Contrast = 0 (max visibility).
    ///
    /// Report ID 1 layout (2-byte Feature Report body after the ID byte):
    ///   Byte 0:  bit 0 = Backlit (1 = on)
    ///            bits 6:1 = Contrast (0 = best, 63 = faded)
    ///   Byte 1:  reserved / padding
    ///
    /// NOTE: the exact layout was inferred from VCS INI config and binary analysis.
    /// If the display remains dark, run the app with the device connected and read the
    /// DumpReportCaps log output to confirm the Feature Report structure.
    /// </summary>
    public static bool InitDisplay(SafeFileHandle handle, bool backlit = true, byte contrast = 0)
    {
        if (handle == null || handle.IsInvalid) return false;
        byte settings = (byte)((backlit ? 1 : 0) | ((contrast & 0x3F) << 1));
        var report = new byte[] { 0x01, settings, 0x00 };
        return HidHelper.HidD_SetFeature(handle, report, (uint)report.Length);
    }
}
