# Commbox Mapper

A Windows system-tray utility for the Commbox USB audio device (VID `0x0000` / PID `0x3200`).
Maps the two PTT footswitches to keyboard keys, gamepad buttons, or mouse buttons — and controls the device's LCD display, sidetone mixer, and volume knobs.

---

## Features

- **PTT switch mapping** — bind either footswitch to any keyboard key (F1–F24, letters, numpad, modifiers, mouse buttons) or Xbox gamepad button via ViGEmBus
- **Hold mode** — hold the key down while the switch is pressed, or tap it once on press
- **Learn mode** — one-click switch detection; press the switch and the app figures out the HID byte/bit automatically
- **LCD display** — always shows `PRIM` / `STBY` on line 1 and your bound key names on line 2; `<` / `>` arrows light up when each switch is pressed
- **Sidetone** — controls the hardware mic-to-headphone mixer via HID Report ID 5 (firmware path) and the WASAPI device topology (Windows path); volume adjustable via the on-device rotary knobs (±3% per click) or the UI slider
- **Sidetone Reset button** — force-resets the hardware mixer if it gets into a bad state after replug
- **Auto-reconnect** — detects unplug and reconnects automatically within 2 seconds; sidetone and display restore without any user action
- **WASAPI volume monitoring** — live output and mic level bars
- **Mono audio** — enables Windows mono mix while running (for headset use)
- **Activity log** — selectable, copyable log with Copy All and Clear buttons
- **System tray** — minimises to tray; optionally starts with Windows

---

## Requirements

| Requirement | Notes |
|---|---|
| Windows 10 / 11 x64 | x64 recommended for published builds |
| Commbox device | VID `0x0000` PID `0x3200` |
| [ViGEmBus driver](https://github.com/nefarius/ViGEmBus/releases) | Optional — required for Xbox gamepad output only |

> The published exe is self-contained; .NET 8 does **not** need to be installed separately.

---

## Installation

1. Download `CommboxMapper.exe` from the [Releases](../../releases) page
2. Place it anywhere (e.g. `C:\Tools\CommboxMapper\`)
3. Run it — it will appear in the system tray
4. Plug in the Commbox; the app connects automatically

To start with Windows: right-click the tray icon → **Start with Windows**.

---

## Switch Setup

1. Click **Learn Switch** under the Left or Right panel
2. Follow the on-screen banner — release the switch if held, then press and hold it
3. The app detects the HID byte and bit automatically
4. Select the action from the dropdown (key, gamepad button, etc.)
5. Check **Hold** if you want the key held down for the duration of the press

---

## Sidetone

Sidetone routes your microphone back into your headset so you can hear yourself.
It is always enabled. Use the **Volume** slider or the rotary knobs on the device to adjust level.

If sidetone stops working after a replug, click **Reset** — this sends a mute then re-enable command to the device firmware.

---

## Building from Source

```
git clone https://github.com/RowanYoung01/CommboxMapper.git
cd CommboxMapper
dotnet build
dotnet run
```

### Publishing (single-file, self-contained)

```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

---

## HID Protocol Reference

| Report ID | Direction | Usage Page | Description |
|---|---|---|---|
| 0x01 | Feature | 0x0014 | Display settings (Backlit, Contrast) |
| 0x02 | Output | 0x0014 | Cursor position — upper nibble = row, lower nibble = col |
| 0x03 | Output | 0x0014 | Character data — 4 ASCII bytes, auto-advances cursor |
| 0x04 | Output | 0x0014 | Display control (blink, bitmap size) |
| 0x05 | Output | 0xFF00 | Sidetone attenuation mixer — 8 bytes, usages 0x20–0x27, range 0–64 (0 = full, 64 = muted) |
| Input | Input | — | Byte 1 = switch bits (B10–B14), Byte 2 = knob bits (B15–B18) |

---

## License

MIT
