using System.Text.Json;
using System.Text.Json.Serialization;

namespace CommboxMapper;

public enum ActionKind { None, Key, GamepadButton }

public record BindEntry(string Label, ActionKind Kind, Keys Key = Keys.None, string? GamepadBtn = null);

public class SwitchConfig
{
    public int    ByteIndex   { get; set; } = -1;
    public byte   BitMask     { get; set; } = 0;
    public bool   ActiveHigh  { get; set; } = true;
    public bool   HoldMode    { get; set; } = true;

    // What action to trigger
    public ActionKind ActionKind    { get; set; } = ActionKind.None;
    public Keys       BoundKey      { get; set; } = Keys.None;
    public string     GamepadButton { get; set; } = "";   // e.g. "A", "B", "LB", "LT"

    [JsonIgnore]
    public bool IsConfigured => ByteIndex >= 0 && BitMask != 0 && ActionKind != ActionKind.None;

    [JsonIgnore]
    public bool IsLearned => ByteIndex >= 0 && BitMask != 0;
}

public class AppSettings
{
    public ushort      DeviceVid         { get; set; } = 0x0000;
    public ushort      DevicePid         { get; set; } = 0x3200;
    public SwitchConfig LeftSwitch       { get; set; } = new();
    public SwitchConfig RightSwitch      { get; set; } = new();
    public bool        DisplayAuto       { get; set; } = true;
    public string      DisplayCustomText { get; set; } = "----";
    public int         LeftSidetoneVolume  { get; set; } = 50;   // 0-100
    public int         RightSidetoneVolume { get; set; } = 50;   // 0-100

    private static string SettingsPath =>
        Path.Combine(
            Path.GetDirectoryName(Environment.ProcessPath ?? AppContext.BaseDirectory) ?? AppContext.BaseDirectory,
            "CommboxMapper.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath)) ?? new();
        }
        catch { }
        return new();
    }

    public void Save()
    {
        try { File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true })); }
        catch { }
    }
}
