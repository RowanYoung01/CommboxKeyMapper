using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaColor = Avalonia.Media.Color;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using Keys = System.Windows.Forms.Keys;
using WF   = System.Windows.Forms;

namespace ACU624KeyMapper.Views;

public partial class MainWindow : Window
{
    // ── Bindable actions ──────────────────────────────────────────────────
    private static readonly BindEntry[] AllBindings =
    [
        new("(none)", ActionKind.None),
        new("F1",  ActionKind.Key, Keys.F1),  new("F2",  ActionKind.Key, Keys.F2),
        new("F3",  ActionKind.Key, Keys.F3),  new("F4",  ActionKind.Key, Keys.F4),
        new("F5",  ActionKind.Key, Keys.F5),  new("F6",  ActionKind.Key, Keys.F6),
        new("F7",  ActionKind.Key, Keys.F7),  new("F8",  ActionKind.Key, Keys.F8),
        new("F9",  ActionKind.Key, Keys.F9),  new("F10", ActionKind.Key, Keys.F10),
        new("F11", ActionKind.Key, Keys.F11), new("F12", ActionKind.Key, Keys.F12),
        new("F13", ActionKind.Key, Keys.F13), new("F14", ActionKind.Key, Keys.F14),
        new("F15", ActionKind.Key, Keys.F15), new("F16", ActionKind.Key, Keys.F16),
        new("F17", ActionKind.Key, Keys.F17), new("F18", ActionKind.Key, Keys.F18),
        new("F19", ActionKind.Key, Keys.F19), new("F20", ActionKind.Key, Keys.F20),
        new("F21", ActionKind.Key, Keys.F21), new("F22", ActionKind.Key, Keys.F22),
        new("F23", ActionKind.Key, Keys.F23), new("F24", ActionKind.Key, Keys.F24),
        new("A",ActionKind.Key,Keys.A), new("B",ActionKind.Key,Keys.B), new("C",ActionKind.Key,Keys.C),
        new("D",ActionKind.Key,Keys.D), new("E",ActionKind.Key,Keys.E), new("F",ActionKind.Key,Keys.F),
        new("G",ActionKind.Key,Keys.G), new("H",ActionKind.Key,Keys.H), new("I",ActionKind.Key,Keys.I),
        new("J",ActionKind.Key,Keys.J), new("K",ActionKind.Key,Keys.K), new("L",ActionKind.Key,Keys.L),
        new("M",ActionKind.Key,Keys.M), new("N",ActionKind.Key,Keys.N), new("O",ActionKind.Key,Keys.O),
        new("P",ActionKind.Key,Keys.P), new("Q",ActionKind.Key,Keys.Q), new("R",ActionKind.Key,Keys.R),
        new("S",ActionKind.Key,Keys.S), new("T",ActionKind.Key,Keys.T), new("U",ActionKind.Key,Keys.U),
        new("V",ActionKind.Key,Keys.V), new("W",ActionKind.Key,Keys.W), new("X",ActionKind.Key,Keys.X),
        new("Y",ActionKind.Key,Keys.Y), new("Z",ActionKind.Key,Keys.Z),
        new("0",ActionKind.Key,Keys.D0), new("1",ActionKind.Key,Keys.D1), new("2",ActionKind.Key,Keys.D2),
        new("3",ActionKind.Key,Keys.D3), new("4",ActionKind.Key,Keys.D4), new("5",ActionKind.Key,Keys.D5),
        new("6",ActionKind.Key,Keys.D6), new("7",ActionKind.Key,Keys.D7), new("8",ActionKind.Key,Keys.D8),
        new("9",ActionKind.Key,Keys.D9),
        new("Numpad 0",ActionKind.Key,Keys.NumPad0), new("Numpad 1",ActionKind.Key,Keys.NumPad1),
        new("Numpad 2",ActionKind.Key,Keys.NumPad2), new("Numpad 3",ActionKind.Key,Keys.NumPad3),
        new("Numpad 4",ActionKind.Key,Keys.NumPad4), new("Numpad 5",ActionKind.Key,Keys.NumPad5),
        new("Numpad 6",ActionKind.Key,Keys.NumPad6), new("Numpad 7",ActionKind.Key,Keys.NumPad7),
        new("Numpad 8",ActionKind.Key,Keys.NumPad8), new("Numpad 9",ActionKind.Key,Keys.NumPad9),
        new("Numpad *",ActionKind.Key,Keys.Multiply), new("Numpad +",ActionKind.Key,Keys.Add),
        new("Numpad -",ActionKind.Key,Keys.Subtract), new("Numpad /",ActionKind.Key,Keys.Divide),
        new("Numpad .",ActionKind.Key,Keys.Decimal),  new("Num Lock",ActionKind.Key,Keys.NumLock),
        new("Enter",ActionKind.Key,Keys.Enter),    new("Space",ActionKind.Key,Keys.Space),
        new("Tab",ActionKind.Key,Keys.Tab),        new("Escape",ActionKind.Key,Keys.Escape),
        new("Backspace",ActionKind.Key,Keys.Back), new("Delete",ActionKind.Key,Keys.Delete),
        new("Insert",ActionKind.Key,Keys.Insert),  new("Home",ActionKind.Key,Keys.Home),
        new("End",ActionKind.Key,Keys.End),        new("Page Up",ActionKind.Key,Keys.Prior),
        new("Page Down",ActionKind.Key,Keys.Next),
        new("Up",ActionKind.Key,Keys.Up), new("Down",ActionKind.Key,Keys.Down),
        new("Left",ActionKind.Key,Keys.Left), new("Right",ActionKind.Key,Keys.Right),
        new("Caps Lock",ActionKind.Key,Keys.CapsLock),
        new("Scroll Lock",ActionKind.Key,Keys.Scroll),
        new("Print Screen",ActionKind.Key,Keys.PrintScreen),
        new("Pause",ActionKind.Key,Keys.Pause),
        new("Left Shift",ActionKind.Key,Keys.LShiftKey), new("Right Shift",ActionKind.Key,Keys.RShiftKey),
        new("Left Ctrl",ActionKind.Key,Keys.LControlKey),new("Right Ctrl",ActionKind.Key,Keys.RControlKey),
        new("Left Alt",ActionKind.Key,Keys.LMenu),       new("Right Alt",ActionKind.Key,Keys.RMenu),
        new("Mouse Button 1 (Left)",   ActionKind.Key, Keys.LButton),
        new("Mouse Button 2 (Right)",  ActionKind.Key, Keys.RButton),
        new("Mouse Button 3 (Middle)", ActionKind.Key, Keys.MButton),
        new("Mouse Button 4 (X1)",     ActionKind.Key, Keys.XButton1),
        new("Mouse Button 5 (X2)",     ActionKind.Key, Keys.XButton2),
        new("Xbox A",            ActionKind.GamepadButton, GamepadBtn: "A"),
        new("Xbox B",            ActionKind.GamepadButton, GamepadBtn: "B"),
        new("Xbox X",            ActionKind.GamepadButton, GamepadBtn: "X"),
        new("Xbox Y",            ActionKind.GamepadButton, GamepadBtn: "Y"),
        new("Xbox LB",           ActionKind.GamepadButton, GamepadBtn: "LB"),
        new("Xbox RB",           ActionKind.GamepadButton, GamepadBtn: "RB"),
        new("Xbox LT (trigger)", ActionKind.GamepadButton, GamepadBtn: "LT"),
        new("Xbox RT (trigger)", ActionKind.GamepadButton, GamepadBtn: "RT"),
        new("Xbox Start",        ActionKind.GamepadButton, GamepadBtn: "Start"),
        new("Xbox Back",         ActionKind.GamepadButton, GamepadBtn: "Back"),
        new("Xbox Guide",        ActionKind.GamepadButton, GamepadBtn: "Guide"),
        new("Xbox LS (click)",   ActionKind.GamepadButton, GamepadBtn: "LS"),
        new("Xbox RS (click)",   ActionKind.GamepadButton, GamepadBtn: "RS"),
        new("Xbox D-Pad Up",     ActionKind.GamepadButton, GamepadBtn: "DPadUp"),
        new("Xbox D-Pad Down",   ActionKind.GamepadButton, GamepadBtn: "DPadDown"),
        new("Xbox D-Pad Left",   ActionKind.GamepadButton, GamepadBtn: "DPadLeft"),
        new("Xbox D-Pad Right",  ActionKind.GamepadButton, GamepadBtn: "DPadRight"),
    ];

    // ── Brushes ───────────────────────────────────────────────────────────
    private static readonly IBrush BrushGreen  = new SolidColorBrush(AvaloniaColor.Parse("#44D282"));
    private static readonly IBrush BrushGray   = new SolidColorBrush(AvaloniaColor.Parse("#606070"));
    private static readonly IBrush BrushOrange = new SolidColorBrush(AvaloniaColor.Parse("#FF9900"));
    private static readonly IBrush BrushRed    = new SolidColorBrush(AvaloniaColor.Parse("#DC4646"));
    private static readonly IBrush BrushMuted  = new SolidColorBrush(AvaloniaColor.Parse("#9090A0"));
    private static readonly IBrush BrushDotOff = new SolidColorBrush(AvaloniaColor.Parse("#2A2E40"));

    // ── App state ─────────────────────────────────────────────────────────
    private AppSettings      _settings = AppSettings.Load();
    private GamepadSimulator _gamepad  = new();
    private CoreAudioHelper? _audio;
    private SidetoneEngine   _sidetone = new(); // re-created with correct VID/PID after settings load

    // ── HID state ─────────────────────────────────────────────────────────
    private SafeFileHandle? _deviceHandle;
    private Thread?         _readThread;
    private volatile bool   _stopReading;
    private bool _leftActive, _rightActive;

    private volatile byte _pendingB1, _pendingB2;
    private volatile bool _hasPendingReport;

    // ── Learn state ───────────────────────────────────────────────────────
    private enum LearnState { Idle, WaitingBaseline, WaitingPress }
    private LearnState _learnState = LearnState.Idle;
    private int        _learnTarget;
    private byte[]?    _baseline;
    private DateTime   _learnStart;

    // ── Tray ──────────────────────────────────────────────────────────────
    private WF.NotifyIcon?        _notifyIcon;
    private WF.ToolStripMenuItem? _startupTrayItem;
    private bool                  _reallyClose;

    private const string StartupRegKey  = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string StartupAppName = "ACU624KeyMapper";

    private static string AppExePath =>
        Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";

    // ── HID monitor dots (created in code) ────────────────────────────────
    private readonly Ellipse[] _buttonDots = new Ellipse[9];

    // ── Log ───────────────────────────────────────────────────────────────
    private readonly ObservableCollection<string> _logItems = new();

    // ── Timers ────────────────────────────────────────────────────────────
    private DispatcherTimer  _volumeTimer    = null!;
    private DispatcherTimer  _monitorTimer   = null!;
    private DispatcherTimer? _reconnectTimer = null;

    public MainWindow()
    {
        InitializeComponent();
        Init();
    }

    private void Init()
    {
        // Re-create sidetone engine with the configured VID/PID from saved settings
        _sidetone = new SidetoneEngine(_settings.DeviceVid, _settings.DevicePid);

        // Populate action dropdowns
        foreach (var e in AllBindings)
        {
            _leftKeyCombo.Items.Add(e.Label);
            _rightKeyCombo.Items.Add(e.Label);
        }
        _leftKeyCombo.SelectedIndex  = 0;
        _rightKeyCombo.SelectedIndex = 0;

        // Wire events
        _reconnectBtn.Click             += (_, _) => TryConnect();
        _leftLearnBtn.Click             += (_, _) => StartLearn(0);
        _rightLearnBtn.Click            += (_, _) => StartLearn(1);
        _leftKeyCombo.SelectionChanged  += (_, _) => OnActionChanged(0);
        _rightKeyCombo.SelectionChanged += (_, _) => OnActionChanged(1);
        _leftHoldChk.IsCheckedChanged   += (_, _) => OnHoldChanged(0);
        _rightHoldChk.IsCheckedChanged  += (_, _) => OnHoldChanged(1);
        _sidetoneSlider.ValueChanged    += (_, _) => OnSidetoneVolumeChanged();
        _sidetoneResetBtn.Click         += (_, _) => ResetSidetone();

        // Log
        _logBox.ItemsSource = _logItems;
        _logCopyBtn.Click  += (_, _) => CopyLogToClipboard();
        _logClearBtn.Click += (_, _) => _logItems.Clear();

        // HID monitor dots
        InitButtonDots();

        // Tray icon
        InitTrayIcon();

        // Timers
        _volumeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _volumeTimer.Tick += (_, _) => UpdateAudioLevels();
        _volumeTimer.Start();

        _monitorTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
        _monitorTimer.Tick += (_, _) => { if (_hasPendingReport) { _hasPendingReport = false; UpdateRawMonitor(); } };
        _monitorTimer.Start();

        // Closing handler
        Closing += OnWindowClosing;

        // Hide to tray on startup only if --minimized is passed (e.g. Windows startup entry)
        Opened += (_, _) =>
        {
            if (Environment.GetCommandLineArgs().Contains("--minimized"))
                HideToTray();
        };

        // Apply saved settings to UI
        RefreshFromSettings();

        if (!_gamepad.IsAvailable)
            Log("ViGEmBus not found — Xbox gamepad output disabled");

        TryConnect();
    }

    private void InitButtonDots()
    {
        // B10-B14 = switches/PTT; B15=Lknob◄ B16=Lknob► B17=Rknob◄ B18=Rknob►
        string[] names = ["B10","B11","B12","B13","B14","L◄","L►","R◄","R►"];
        for (int i = 0; i < 9; i++)
        {
            var panel = new StackPanel { Spacing = 2, Margin = new Avalonia.Thickness(0, 0, 8, 0) };
            var dot   = new Ellipse { Width = 14, Height = 14, Fill = BrushDotOff };
            var lbl   = new TextBlock { Text = names[i], FontSize = 10, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, Foreground = BrushMuted };
            panel.Children.Add(dot);
            panel.Children.Add(lbl);
            _buttonContainer.Children.Add(panel);
            _buttonDots[i] = dot;
        }
    }

    private void InitTrayIcon()
    {
        var icon = System.Drawing.Icon.ExtractAssociatedIcon(AppExePath) ?? System.Drawing.SystemIcons.Application;

        var cm = new WF.ContextMenuStrip();
        cm.Items.Add(new WF.ToolStripMenuItem("ACU-624D Key Mapper") { Enabled = false, Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold) });
        cm.Items.Add(new WF.ToolStripSeparator());
        cm.Items.Add(new WF.ToolStripMenuItem("Open", null, (_, _) => Dispatcher.UIThread.Post(ShowWindow)));
        _startupTrayItem = new WF.ToolStripMenuItem("Start with Windows", null, OnToggleStartup) { Checked = IsInStartup(), CheckOnClick = false };
        cm.Items.Add(_startupTrayItem);
        cm.Items.Add(new WF.ToolStripSeparator());
        cm.Items.Add(new WF.ToolStripMenuItem("Exit", null, (_, _) => Dispatcher.UIThread.Post(ExitApp)));

        _notifyIcon = new WF.NotifyIcon { Text = "ACU-624D Key Mapper", Icon = icon, Visible = true, ContextMenuStrip = cm };
        _notifyIcon.DoubleClick += (_, _) => Dispatcher.UIThread.Post(ShowWindow);
    }

    // ── Settings → UI ─────────────────────────────────────────────────────
    private void RefreshFromSettings()
    {
        SetCombo(_leftKeyCombo,  _settings.LeftSwitch);
        SetCombo(_rightKeyCombo, _settings.RightSwitch);
        _leftHoldChk.IsChecked  = _settings.LeftSwitch.HoldMode;
        _rightHoldChk.IsChecked = _settings.RightSwitch.HoldMode;
        _sidetoneSlider.Value = Math.Clamp(_settings.SidetoneVolume, 0, 100);
        _sidetoneLbl.Text     = $"{_settings.SidetoneVolume} %";
        UpdateConfigLabel(0);
        UpdateConfigLabel(1);
    }

    private static void SetCombo(Avalonia.Controls.ComboBox cb, SwitchConfig cfg)
    {
        for (int i = 0; i < AllBindings.Length; i++)
        {
            var e = AllBindings[i];
            bool match = (cfg.ActionKind == ActionKind.None && e.Kind == ActionKind.None) ||
                         (cfg.ActionKind == ActionKind.Key && e.Kind == ActionKind.Key && e.Key == cfg.BoundKey) ||
                         (cfg.ActionKind == ActionKind.GamepadButton && e.Kind == ActionKind.GamepadButton && e.GamepadBtn == cfg.GamepadButton);
            if (match) { cb.SelectedIndex = i; return; }
        }
        cb.SelectedIndex = 0;
    }

    private void UpdateConfigLabel(int idx)
    {
        var cfg = idx == 0 ? _settings.LeftSwitch : _settings.RightSwitch;
        var lbl = idx == 0 ? _leftConfigLbl : _rightConfigLbl;
        lbl.Text       = cfg.IsLearned ? $"Byte {cfg.ByteIndex}, mask 0x{cfg.BitMask:X2}  ·  {(cfg.ActiveHigh ? "Active high" : "Active low")}" : "Not learned yet";
        lbl.Foreground = cfg.IsLearned ? BrushGreen : BrushGray;
    }

    // ── Connection ────────────────────────────────────────────────────────
    private void TryConnect()
    {
        _reconnectTimer?.Stop();
        _reconnectTimer = null;
        StopReading();
        _audio?.Dispose();
        _audio = null;
        SetStatus("Searching…", BrushOrange, "");

        var paths = HidHelper.FindDevicePaths(_settings.DeviceVid, _settings.DevicePid);
        if (paths.Count == 0)
        {
            SetStatus("Not found", BrushRed, "Check USB connection  (VID:0000 PID:3200)");
            Log("Device not found");
            return;
        }

        var info = HidHelper.GetDeviceInfo(paths[0]);
        string infoText = info != null
            ? $"{info.Model}  ·  S/N: {info.Serial}  ·  FW: {info.Firmware}  ·  {info.Manufacturer}"
            : $"VID:{_settings.DeviceVid:X4}  PID:{_settings.DevicePid:X4}";

        _deviceHandle = HidHelper.OpenDevice(paths[0]);
        if (_deviceHandle.IsInvalid)
        {
            SetStatus("Found but cannot open", BrushRed, infoText);
            Log("Open failed — device may be in exclusive use by another app");
            return;
        }

        SetStatus("Connected", BrushGreen, infoText);
        Log($"Connected: {infoText}");

        // Cache output/feature report lengths so writes are padded correctly
        DeviceDisplay.Setup(_deviceHandle);

        // Restore endpoint and topology volumes to max — repairs any previously-lowered nodes
        _sidetone.RestoreAllVolumes();

        // Enable Windows mono audio while the app is running
        SetMonoAudio(true);
        Log("Mono audio enabled");

        // Dump HID report descriptor to log so we can see all VCS-SU reports
        Task.Run(() =>
        {
            string caps = HidHelper.DumpReportCaps(paths[0]);
            Dispatcher.UIThread.Post(() =>
            {
                foreach (var line in caps.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                    Log(line.TrimEnd());
            });
        });

        _audio = new CoreAudioHelper(_settings.DeviceVid, _settings.DevicePid);
        if (!_audio.IsAvailable)
            Log("Audio endpoint not found — volume monitoring unavailable");

        // Initialize LCD backlight and contrast (Backlit=1, Contrast=0 — same as VCSPosition)
        DeviceDisplay.InitDisplay(_deviceHandle);

        SendDisplayText();

        // Sidetone is always active. Apply immediately; retry topology if endpoint not ready yet.
        ApplySidetone();
        if (_sidetone.LastError?.Contains("capture device not found") == true)
        {
            Log("Sidetone: audio endpoint not ready yet, will retry…");
            RetryHardwareSidetone(attemptsLeft: 5);
        }

        StartReadThread();
    }

    private void StartReadThread()
    {
        _stopReading = false;
        _readThread  = new Thread(ReadLoop) { IsBackground = true, Name = "HidRead" };
        _readThread.Start();
    }

    private void RetryHardwareSidetone(int attemptsLeft)
    {
        if (attemptsLeft <= 0) return;
        Task.Delay(2000).ContinueWith(_ =>
            Dispatcher.UIThread.Post(() =>
            {
                if (_deviceHandle == null || _deviceHandle.IsInvalid) return;
                Log($"Sidetone retry (attempts left: {attemptsLeft - 1})…");
                ApplySidetone();
                if (_sidetone.LastError?.Contains("capture device not found") == true)
                    RetryHardwareSidetone(attemptsLeft - 1);
            }));
    }

    private void StopReading()
    {
        _stopReading = true;
        _sidetone.StopSoftware();
        DeviceDisplay.Reset();
        _deviceHandle?.Close();
        _deviceHandle = null;
        _readThread?.Join(500);
        _readThread = null;
        ReleaseIfHeld(ref _leftActive,  _settings.LeftSwitch);
        ReleaseIfHeld(ref _rightActive, _settings.RightSwitch);
    }

    private void StartAutoReconnect()
    {
        _reconnectTimer?.Stop();
        _reconnectTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _reconnectTimer.Tick += (_, _) =>
        {
            var paths = HidHelper.FindDevicePaths(_settings.DeviceVid, _settings.DevicePid);
            if (paths.Count > 0)
            {
                _reconnectTimer?.Stop();
                _reconnectTimer = null;
                Log("Device detected — reconnecting…");
                TryConnect();
            }
        };
        _reconnectTimer.Start();
    }

    // ── HID read loop ─────────────────────────────────────────────────────
    private void ReadLoop()
    {
        var buf    = new byte[65];
        byte prevB1 = 0, prevB2 = 0;

        while (!_stopReading && _deviceHandle is { IsInvalid: false })
        {
            uint read = 0;
            if (!HidHelper.ReadFile(_deviceHandle, buf, (uint)buf.Length, ref read, IntPtr.Zero) || read == 0)
            {
                if (!_stopReading)
                    Dispatcher.UIThread.Post(() =>
                    {
                        SetStatus("Disconnected", BrushRed, "");
                        Log("Device disconnected — will reconnect automatically");
                        StartAutoReconnect();
                    });
                break;
            }

            byte b1 = read > 1 ? buf[1] : (byte)0;
            byte b2 = read > 2 ? buf[2] : (byte)0;
            _pendingB1 = b1; _pendingB2 = b2; _hasPendingReport = true;

            if (b1 != prevB1 || b2 != prevB2 || _learnState != LearnState.Idle)
            {
                prevB1 = b1; prevB2 = b2;
                var report = buf[..(int)read];
                Dispatcher.UIThread.Post(() => ProcessReport(report));
            }
        }
    }

    // ── Report processing ─────────────────────────────────────────────────
    private void ProcessReport(byte[] report)
    {
        if (_learnState == LearnState.WaitingBaseline)
        {
            _baseline   = (byte[])report.Clone();
            _learnState = LearnState.WaitingPress;
            _learnInfoLabel.Text = $"Now PRESS and HOLD the {(_learnTarget == 0 ? "LEFT" : "RIGHT")} switch…";
            Log("Baseline captured — press the switch now");
            return;
        }
        if (_learnState == LearnState.WaitingPress)
        {
            if (_baseline != null && ReportsDiffer(_baseline, report, out int byteIdx, out byte mask, out bool activeHigh))
            {
                var cfg = _learnTarget == 0 ? _settings.LeftSwitch : _settings.RightSwitch;
                cfg.ByteIndex = byteIdx; cfg.BitMask = mask; cfg.ActiveHigh = activeHigh;
                _settings.Save();
                _learnState = LearnState.Idle;
                _learnBanner.IsVisible = false;
                UpdateConfigLabel(_learnTarget);
                Log($"{(_learnTarget == 0 ? "Left" : "Right")} learned: byte {byteIdx}, mask 0x{mask:X2}");
                _baseline = null;
            }
            else if ((DateTime.Now - _learnStart).TotalSeconds > 10)
            {
                _learnState = LearnState.Idle;
                _learnBanner.IsVisible = false;
                Log("Learn timed out");
            }
            return;
        }

        CheckSwitch(report, _settings.LeftSwitch,  ref _leftActive,  "Left");
        CheckSwitch(report, _settings.RightSwitch, ref _rightActive, "Right");

        // Knob events — byte 2 bits 0-3: B15=L◄ B16=L► B17=R◄ B18=R►
        // Both knobs adjust sidetone volume (left knob = coarse, right = fine)
        if (report.Length > 2)
        {
            byte b2 = report[2];
            if ((b2 & 0x01) != 0) AdjustSidetone(-3);  // B15 left knob CCW
            if ((b2 & 0x02) != 0) AdjustSidetone(+3);  // B16 left knob CW
            if ((b2 & 0x04) != 0) AdjustSidetone(-3);  // B17 right knob CCW
            if ((b2 & 0x08) != 0) AdjustSidetone(+3);  // B18 right knob CW
        }
    }

    private void AdjustSidetone(int delta)
    {
        int newVol = Math.Clamp(_settings.SidetoneVolume + delta, 0, 100);
        if (newVol == _settings.SidetoneVolume) return;

        _settings.SidetoneVolume = newVol;
        _sidetoneSlider.Value    = newVol;
        _sidetoneLbl.Text        = $"{newVol} %";

        _sidetone.TrySetHardwareSidetone(true, newVol);
        if (_deviceHandle != null && !_deviceHandle.IsInvalid)
            SidetoneEngine.TryHidSidetone(_deviceHandle, true, newVol);
        Log($"Sidetone {newVol} %");
        _settings.Save();
    }

    // ── Raw monitor (30 Hz timer) ─────────────────────────────────────────
    private void UpdateRawMonitor()
    {
        byte b1 = _pendingB1, b2 = _pendingB2;
        _rawBytesLbl.Text = $"Bytes:  {b1:X2}  {b2:X2}";
        bool[] pressed = new bool[9];
        for (int i = 0; i < 5; i++) pressed[i]     = (b1 & (1 << i)) != 0;
        for (int i = 0; i < 4; i++) pressed[5 + i] = (b2 & (1 << i)) != 0;
        for (int i = 0; i < 9; i++)
            _buttonDots[i].Fill = pressed[i] ? BrushGreen : BrushDotOff;
    }

    private void CheckSwitch(byte[] report, SwitchConfig cfg, ref bool wasActive, string name)
    {
        if (!cfg.IsLearned || cfg.ByteIndex >= report.Length) return;
        bool bitSet   = (report[cfg.ByteIndex] & cfg.BitMask) != 0;
        bool isActive = cfg.ActiveHigh ? bitSet : !bitSet;
        if (isActive == wasActive) return;
        wasActive = isActive;

        var lbl = name == "Left" ? _leftStateLbl : _rightStateLbl;

        if (isActive)
        {
            lbl.Text       = "PRESSED";
            lbl.Foreground = BrushGreen;
            var label = AllBindings.FirstOrDefault(b =>
                b.Kind == cfg.ActionKind &&
                (cfg.ActionKind == ActionKind.Key ? b.Key == cfg.BoundKey : b.GamepadBtn == cfg.GamepadButton))?.Label ?? "?";
            Log($"{name} PRESSED → {label}");
            FireAction(cfg, down: true);
        }
        else
        {
            lbl.Text       = "RELEASED";
            lbl.Foreground = BrushGray;
            Log($"{name} RELEASED");
            if (cfg.HoldMode) FireAction(cfg, down: false);
        }

    }

    // ── Device display ────────────────────────────────────────────────────
    // Line 1: "PRIM STBY" — fixed status label (matches VCSPosition convention)
    // Line 2: left key name (left-justified) + right key name (right-justified) in 16 chars
    private void SendDisplayText()
    {
        string line1 = "<PRIM      STBY>"[..16];
        string left  = GetSwitchLabel(0);
        string right = GetSwitchLabel(1);
        // Fit both labels into 16 chars: left-justified left, right-justified right
        string line2 = $"<{left,-7}{right,7}>".PadRight(16)[..16];

        _displayPreview.Text = line1.TrimEnd() + "\n" + line2.TrimEnd();

        if (_deviceHandle is { IsInvalid: false } hdl)
        {
            string l1 = line1, l2 = line2;
            Task.Run(() => { try { DeviceDisplay.Write2Lines(hdl, l1, l2); } catch { } });
        }
    }

    private string GetSwitchLabel(int idx)
    {
        var cfg = idx == 0 ? _settings.LeftSwitch : _settings.RightSwitch;
        if (cfg.ActionKind == ActionKind.Key)
        {
            string key = cfg.BoundKey.ToString();
            return key[..Math.Min(key.Length, 7)];
        }
        if (cfg.ActionKind == ActionKind.GamepadButton)
        {
            string btn = cfg.GamepadButton ?? "BTN";
            return btn[..Math.Min(btn.Length, 7)];
        }
        return idx == 0 ? "LEFT   " : "RIGHT  ";
    }

    // ── Audio levels ──────────────────────────────────────────────────────
    private void UpdateAudioLevels()
    {
        if (_audio == null) { _outVolLbl.Text = "N/A"; _inVolLbl.Text = "N/A"; return; }
        int outVol = _audio.GetOutputVolume();
        int inVol  = _audio.GetInputVolume();
        if (outVol >= 0) { _outVolBar.Value = Math.Clamp(outVol, 0, 100); _outVolLbl.Text = $"{outVol} %"; }
        else _outVolLbl.Text = "N/A";
        if (inVol  >= 0) { _inVolBar.Value  = Math.Clamp(inVol,  0, 100); _inVolLbl.Text  = $"{inVol} %"; }
        else _inVolLbl.Text = "N/A";
    }

    // ── Action changed ────────────────────────────────────────────────────
    private void OnActionChanged(int idx)
    {
        var combo = idx == 0 ? _leftKeyCombo : _rightKeyCombo;
        if (combo.SelectedIndex < 0 || combo.SelectedIndex >= AllBindings.Length) return;
        var entry = AllBindings[combo.SelectedIndex];
        var cfg   = idx == 0 ? _settings.LeftSwitch : _settings.RightSwitch;
        cfg.ActionKind = entry.Kind; cfg.BoundKey = entry.Key; cfg.GamepadButton = entry.GamepadBtn;
        if (entry.Kind == ActionKind.GamepadButton && !_gamepad.IsAvailable)
            Log("Warning: Xbox output requires ViGEmBus driver");
        _settings.Save();
        Log($"{(idx == 0 ? "Left" : "Right")} action → {entry.Label}");
        SendDisplayText();
    }

    private void OnHoldChanged(int idx)
    {
        var chk = idx == 0 ? _leftHoldChk : _rightHoldChk;
        var cfg = idx == 0 ? _settings.LeftSwitch : _settings.RightSwitch;
        cfg.HoldMode = chk.IsChecked == true;
        _settings.Save();
    }

    // ── Misc ──────────────────────────────────────────────────────────────
    private void FireAction(SwitchConfig cfg, bool down)
    {
        if (cfg.ActionKind == ActionKind.None) return;
        if (cfg.ActionKind == ActionKind.GamepadButton)
        {
            if (down) _gamepad.ButtonDown(cfg.GamepadButton);
            else      _gamepad.ButtonUp(cfg.GamepadButton);
            return;
        }
        if (cfg.BoundKey == Keys.None) return;
        if (down)
        {
            if (cfg.HoldMode) InputSimulator.KeyDown(cfg.BoundKey);
            else Task.Run(async () => { InputSimulator.KeyDown(cfg.BoundKey); await Task.Delay(50); InputSimulator.KeyUp(cfg.BoundKey); });
        }
        else InputSimulator.KeyUp(cfg.BoundKey);
    }

    private static bool ReportsDiffer(byte[] baseline, byte[] current, out int byteIdx, out byte mask, out bool activeHigh)
    {
        byteIdx = -1; mask = 0; activeHigh = true;
        for (int i = 1; i < Math.Min(baseline.Length, current.Length); i++)
        {
            byte diff = (byte)(baseline[i] ^ current[i]);
            if (diff == 0) continue;
            byte bit = (byte)(diff & (byte)(-(int)diff));
            byteIdx = i; mask = bit; activeHigh = (current[i] & bit) != 0;
            return true;
        }
        return false;
    }

    private void ReleaseIfHeld(ref bool wasActive, SwitchConfig cfg)
    {
        if (wasActive && cfg.HoldMode) FireAction(cfg, down: false);
        wasActive = false;
    }

    private void StartLearn(int idx)
    {
        if (_deviceHandle == null || _deviceHandle.IsInvalid)
        {
            Log("Cannot learn — device not connected");
            return;
        }
        _learnTarget = idx; _learnState = LearnState.WaitingBaseline;
        _learnStart  = DateTime.Now; _baseline = null;
        _learnInfoLabel.Text   = $"Learning {(idx == 0 ? "LEFT" : "RIGHT")} switch — RELEASE it if currently held…";
        _learnBanner.IsVisible = true;
        Log($"Learn started for {(idx == 0 ? "Left" : "Right")} switch");
    }

    // ── Tray / window ─────────────────────────────────────────────────────
    private void HideToTray()
    {
        Hide();
    }

    private void ShowWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void ExitApp()
    {
        _reallyClose = true;
        Close();
    }

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (!_reallyClose) { e.Cancel = true; HideToTray(); return; }
        _monitorTimer.Stop();
        _volumeTimer.Stop();
        _reconnectTimer?.Stop();
        StopReading();
        DeviceDisplay.Clear(_deviceHandle!);
        _settings.Save();
        _sidetone.StopSoftware();
        _gamepad.Dispose();
        _audio?.Dispose();
        if (_notifyIcon != null) { _notifyIcon.Visible = false; _notifyIcon.Dispose(); }
        SetMonoAudio(false);
    }

    private static bool IsInStartup()
    {
        using var key = Registry.CurrentUser.OpenSubKey(StartupRegKey, false);
        return key?.GetValue(StartupAppName) is string val &&
               val.Trim('"').Equals(AppExePath, StringComparison.OrdinalIgnoreCase);
    }

    private void OnToggleStartup(object? sender, EventArgs e)
    {
        using var key = Registry.CurrentUser.OpenSubKey(StartupRegKey, true);
        if (key == null) return;
        if (IsInStartup())
        {
            key.DeleteValue(StartupAppName, false);
            if (_startupTrayItem != null) _startupTrayItem.Checked = false;
            Log("Removed from startup");
        }
        else
        {
            key.SetValue(StartupAppName, $"\"{AppExePath}\"");
            if (_startupTrayItem != null) _startupTrayItem.Checked = true;
            Log("Added to startup");
        }
    }

    // ── Status bar ────────────────────────────────────────────────────────
    private void SetStatus(string text, IBrush dotColor, string info)
    {
        _statusLabel.Text      = text;
        _statusDot.Fill        = dotColor;
        _deviceInfoLabel.Text  = info;
    }

    // ── Sidetone ──────────────────────────────────────────────────────────
    private void ResetSidetone()
    {
        Log("Sidetone reset…");
        // Send full mute first, then re-enable — mimics the old toggle behaviour
        if (_deviceHandle != null && !_deviceHandle.IsInvalid)
            SidetoneEngine.TryHidSidetone(_deviceHandle, false, 0);
        ApplySidetone();
    }

    private void ApplySidetone()
    {
        // 1. WASAPI device-topology path — unmutes the Windows-side audio node
        bool hwOk = _sidetone.TrySetHardwareSidetone(true, _settings.SidetoneVolume);
        if (!hwOk && _sidetone.LastError != null)
            Log($"Sidetone HW topology error: {_sidetone.LastError}");

        // 2. HID Report ID 5 — controls the ACU-624D firmware's hardware sidetone mixer.
        //    Always send this; it is what actually routes mic audio to headphone output.
        bool hidOk = false;
        if (_deviceHandle != null && !_deviceHandle.IsInvalid)
            hidOk = SidetoneEngine.TryHidSidetone(_deviceHandle, true, _settings.SidetoneVolume);

        string status = hidOk ? $"Sidetone active ({_settings.SidetoneVolume}%)"
                      : hwOk  ? $"Sidetone active — topology only ({_settings.SidetoneVolume}%)"
                      :          "Failed — check ACU-624D is connected";
        _sidetoneStatus.Text       = status;
        _sidetoneStatus.Foreground = (hwOk || hidOk) ? BrushGreen : BrushRed;
        Log($"Sidetone: {status}");
    }

    private void OnSidetoneVolumeChanged()
    {
        int pct = (int)_sidetoneSlider.Value;
        _sidetoneLbl.Text        = $"{pct} %";
        _settings.SidetoneVolume = pct;
        _settings.Save();
        _sidetone.TrySetHardwareSidetone(true, pct);
        if (_deviceHandle != null && !_deviceHandle.IsInvalid)
            SidetoneEngine.TryHidSidetone(_deviceHandle, true, pct);
    }

    // ── Mono audio ────────────────────────────────────────────────────────
    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern bool SendNotifyMessage(IntPtr hWnd, uint msg, UIntPtr wParam, string lParam);
    private const uint WM_SETTINGCHANGE = 0x001A;

    private static void SetMonoAudio(bool mono)
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser
                .CreateSubKey(@"Software\Microsoft\Multimedia\Audio");
            key.SetValue("AccessibilityMonoMixState", mono ? 1 : 0,
                Microsoft.Win32.RegistryValueKind.DWord);
            // Broadcast the change so Windows applies it immediately
            SendNotifyMessage((IntPtr)0xFFFF, WM_SETTINGCHANGE, UIntPtr.Zero, "Accessibility");
        }
        catch { }
    }

    // ── Log ───────────────────────────────────────────────────────────────
    private void Log(string msg)
    {
        _logItems.Insert(0, $"{DateTime.Now:HH:mm:ss}  {msg}");
        if (_logItems.Count > 200) _logItems.RemoveAt(_logItems.Count - 1);
    }

    private async void CopyLogToClipboard()
    {
        var text = string.Join(Environment.NewLine, _logItems);
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard != null)
            await clipboard.SetTextAsync(text);
    }
}
