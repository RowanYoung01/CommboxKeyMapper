using System.Runtime.InteropServices;

namespace ACU624KeyMapper;

/// <summary>
/// Simulates keyboard key presses and mouse button clicks via SendInput.
/// Keys.LButton / MButton / RButton / XButton1 / XButton2 are sent as mouse events;
/// everything else is sent as a keyboard event.
/// </summary>
public static class InputSimulator
{
    // ── Input type constants ──────────────────────────────────────────────
    private const uint INPUT_MOUSE    = 0;
    private const uint INPUT_KEYBOARD = 1;

    // ── Keyboard flags ────────────────────────────────────────────────────
    private const uint KEYEVENTF_KEYUP = 0x0002;

    // ── Mouse event flags ─────────────────────────────────────────────────
    private const uint MOUSEEVENTF_LEFTDOWN   = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP     = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN  = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP    = 0x0010;
    private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    private const uint MOUSEEVENTF_MIDDLEUP   = 0x0040;
    private const uint MOUSEEVENTF_XDOWN      = 0x0080;
    private const uint MOUSEEVENTF_XUP        = 0x0100;
    private const uint XBUTTON1               = 0x0001;
    private const uint XBUTTON2               = 0x0002;

    // ── Structs ───────────────────────────────────────────────────────────
    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint   dwFlags;
        public uint   time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int    dx, dy;
        public uint   mouseData;
        public uint   dwFlags;
        public uint   time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT  mi;
        [FieldOffset(0)] public KEYBDINPUT  ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint       type;
        public InputUnion data;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern ushort MapVirtualKey(uint uCode, uint uMapType);

    // ── Public API ────────────────────────────────────────────────────────
    public static void KeyDown(Keys key)  => Send(key, down: true);
    public static void KeyUp(Keys key)    => Send(key, down: false);

    private static void Send(Keys key, bool down)
    {
        INPUT input;

        switch (key)
        {
            case Keys.LButton:
                input = MouseInput(down ? MOUSEEVENTF_LEFTDOWN   : MOUSEEVENTF_LEFTUP,   0);
                break;
            case Keys.RButton:
                input = MouseInput(down ? MOUSEEVENTF_RIGHTDOWN  : MOUSEEVENTF_RIGHTUP,  0);
                break;
            case Keys.MButton:
                input = MouseInput(down ? MOUSEEVENTF_MIDDLEDOWN : MOUSEEVENTF_MIDDLEUP, 0);
                break;
            case Keys.XButton1:
                input = MouseInput(down ? MOUSEEVENTF_XDOWN : MOUSEEVENTF_XUP, XBUTTON1);
                break;
            case Keys.XButton2:
                input = MouseInput(down ? MOUSEEVENTF_XDOWN : MOUSEEVENTF_XUP, XBUTTON2);
                break;
            default:
                input = KeyInput(key, down);
                break;
        }

        SendInput(1, [input], Marshal.SizeOf<INPUT>());
    }

    private static INPUT KeyInput(Keys key, bool keyUp)
    {
        ushort scan = MapVirtualKey((uint)key, 0);
        return new INPUT
        {
            type = INPUT_KEYBOARD,
            data = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk    = (ushort)key,
                    wScan  = scan,
                    dwFlags = keyUp ? KEYEVENTF_KEYUP : 0u
                }
            }
        };
    }

    private static INPUT MouseInput(uint flags, uint mouseData) => new()
    {
        type = INPUT_MOUSE,
        data = new InputUnion
        {
            mi = new MOUSEINPUT { dwFlags = flags, mouseData = mouseData }
        }
    };
}
