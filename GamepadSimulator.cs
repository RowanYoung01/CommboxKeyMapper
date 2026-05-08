using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace CommboxMapper;

/// <summary>
/// Creates a virtual Xbox 360 controller via ViGEmBus.
/// If ViGEmBus is not installed, IsAvailable will be false and all calls are no-ops.
/// Install ViGEmBus from: https://github.com/nefarius/ViGEmBus/releases
/// </summary>
public sealed class GamepadSimulator : IDisposable
{
    private ViGEmClient?         _client;
    private IXbox360Controller?  _pad;

    public bool IsAvailable { get; private set; }

    // Maps the string key stored in settings → Xbox360Button
    public static readonly Dictionary<string, Xbox360Button> ButtonMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["A"]          = Xbox360Button.A,
            ["B"]          = Xbox360Button.B,
            ["X"]          = Xbox360Button.X,
            ["Y"]          = Xbox360Button.Y,
            ["LB"]         = Xbox360Button.LeftShoulder,
            ["RB"]         = Xbox360Button.RightShoulder,
            ["Start"]      = Xbox360Button.Start,
            ["Back"]       = Xbox360Button.Back,
            ["Guide"]      = Xbox360Button.Guide,
            ["LS"]         = Xbox360Button.LeftThumb,
            ["RS"]         = Xbox360Button.RightThumb,
            ["DPadUp"]     = Xbox360Button.Up,
            ["DPadDown"]   = Xbox360Button.Down,
            ["DPadLeft"]   = Xbox360Button.Left,
            ["DPadRight"]  = Xbox360Button.Right,
        };

    public static readonly Dictionary<string, Xbox360Slider> SliderMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["LT"] = Xbox360Slider.LeftTrigger,
            ["RT"] = Xbox360Slider.RightTrigger,
        };

    public GamepadSimulator()
    {
        try
        {
            _client = new ViGEmClient();
            _pad    = _client.CreateXbox360Controller();
            _pad.Connect();
            IsAvailable = true;
        }
        catch
        {
            IsAvailable = false;
        }
    }

    public void ButtonDown(string name)
    {
        if (!IsAvailable || _pad == null) return;
        if (ButtonMap.TryGetValue(name, out var btn))
            _pad.SetButtonState(btn, true);
        else if (SliderMap.TryGetValue(name, out var sl))
            _pad.SetSliderValue(sl, 255);
    }

    public void ButtonUp(string name)
    {
        if (!IsAvailable || _pad == null) return;
        if (ButtonMap.TryGetValue(name, out var btn))
            _pad.SetButtonState(btn, false);
        else if (SliderMap.TryGetValue(name, out var sl))
            _pad.SetSliderValue(sl, 0);
    }

    public void Dispose()
    {
        try { _pad?.Disconnect(); } catch { }
        _client?.Dispose();
    }
}
