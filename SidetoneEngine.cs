using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Microsoft.Win32.SafeHandles;

namespace ACU624KeyMapper;

/// <summary>
/// Two-path sidetone implementation for the ACU-624D:
///
/// 1. Hardware path: sends HID Output Report ID 4 to the device.
///    The report contains a 2-bit enable flag (usage 0x46) and a 6-bit level (usage 0x47, 0-127).
///    This may directly control the device's internal sidetone circuit.
///
/// 2. Software path (WASAPI): captures the ACU-624D microphone (capture endpoint)
///    and plays it back through the ACU-624D headset output (render endpoint) with
///    configurable volume and ~50 ms latency.  This works even if the hardware path
///    has no effect.
/// </summary>
public sealed class SidetoneEngine : IDisposable
{
    // ── State ─────────────────────────────────────────────────────────────
    private WasapiCapture?          _capture;
    private WasapiOut?              _player;
    private BufferedWaveProvider?   _buffer;
    private VolumeSampleProvider?   _volumeProvider;
    private bool                    _softwareRunning;

    public bool   IsRunning       => _softwareRunning;
    public float  SoftwareVolume  { get; private set; } = 0.5f; // 0.0-1.0

    // ── Constant for hardware report ──────────────────────────────────────
    // Report ID 4 layout (1 byte data after the ID byte):
    //   bits [1:0]  = enable field (usage 0x46, logical max 1)
    //   bits [7:2]  = level  field (usage 0x47, logical max 127 but in 6 bits = 0-63)
    private const byte ReportId4 = 0x04;

    // ── WASAPI device discovery ───────────────────────────────────────────
    private const string VidPid = "VID_0000&PID_3200";

    private static MMDevice? FindDevice(DataFlow flow)
    {
        using var enumerator = new MMDeviceEnumerator();
        var collection = enumerator.EnumerateAudioEndPoints(flow, DeviceState.Active);
        foreach (var dev in collection)
        {
            if (dev.ID.Contains(VidPid, StringComparison.OrdinalIgnoreCase))
                return dev;
        }
        return null;
    }

    // ── Hardware path ─────────────────────────────────────────────────────
    /// <summary>
    /// Sends HID Report ID 4 to the device to attempt hardware sidetone control.
    /// enable: true to enable, false to disable.
    /// level:  0-63 (maps to the 6-bit field, logical max 63).
    /// </summary>
    public static bool TrySendHardwareReport(SafeFileHandle handle, bool enable, int level)
    {
        if (handle == null || handle.IsInvalid) return false;

        // Pack: bits [1:0] = enable (0 or 1), bits [7:2] = level (0-63)
        byte enableBits = enable ? (byte)1 : (byte)0;
        byte levelBits  = (byte)(Math.Clamp(level, 0, 63) << 2);
        byte data       = (byte)(enableBits | levelBits);

        var report = new byte[] { ReportId4, data };
        uint written = 0;
        return HidHelper.WriteFile(handle, report, (uint)report.Length, ref written, IntPtr.Zero);
    }

    // ── Software path ─────────────────────────────────────────────────────
    public bool StartSoftware(float volume = 0.5f)
    {
        StopSoftware();

        var micDevice  = FindDevice(DataFlow.Capture);
        var spkDevice  = FindDevice(DataFlow.Render);

        if (micDevice == null || spkDevice == null) return false;

        try
        {
            _capture = new WasapiCapture(micDevice, true, 50);
            _player  = new WasapiOut(spkDevice, AudioClientShareMode.Shared, true, 50);

            var waveFormat = _capture.WaveFormat;
            _buffer = new BufferedWaveProvider(waveFormat) { DiscardOnBufferOverflow = true };

            // Convert to float samples for volume control
            IWaveProvider source = _buffer;
            if (waveFormat.Encoding == WaveFormatEncoding.Extensible ||
                waveFormat.BitsPerSample == 16)
            {
                var toFloat = new Wave16ToFloatProvider(source);
                _volumeProvider = new VolumeSampleProvider(toFloat.ToSampleProvider());
            }
            else
            {
                _volumeProvider = new VolumeSampleProvider(source.ToSampleProvider());
            }

            _volumeProvider.Volume = volume;
            SoftwareVolume = volume;

            _player.Init(_volumeProvider);
            _capture.DataAvailable += OnCaptureData;

            _player.Play();
            _capture.StartRecording();
            _softwareRunning = true;
            return true;
        }
        catch
        {
            StopSoftware();
            return false;
        }
    }

    private void OnCaptureData(object? sender, WaveInEventArgs e)
    {
        _buffer?.AddSamples(e.Buffer, 0, e.BytesRecorded);
    }

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
        _buffer = null;
        _volumeProvider = null;
        _softwareRunning = false;
    }

    public void Dispose() => StopSoftware();
}
