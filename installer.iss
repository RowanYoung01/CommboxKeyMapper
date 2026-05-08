#define AppName     "Commbox Mapper"
#define AppExe      "CommboxMapper.exe"
#define AppVersion  "1.0.0"
#define AppPublisher "Rowan"
#define SourceDir   "publish"

[Setup]
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppId={{A3F1C2B4-7D8E-4F9A-B012-3456789ABCDE}
DefaultDirName={localappdata}\CommboxMapper
DisableDirPage=yes
DefaultGroupName={#AppName}
OutputDir=installer_output
OutputBaseFilename=CommboxMapper_Setup
Compression=lzma2/ultra64
SolidCompression=yes
PrivilegesRequired=lowest
WizardStyle=modern
SetupIconFile=
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\{#AppExe}
; Start the app after install (passes --show so the window opens once)
; Startup entry is managed by the app itself via the tray menu

[Files]
Source: "{#SourceDir}\{#AppExe}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Tasks]
Name: desktopicon; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"
Name: startupentry; Description: "Start {#AppName} automatically when Windows starts"; GroupDescription: "Startup:"

[Registry]
; Add to startup if the user checked the task
Root: HKCU; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Run"; \
    ValueType: string; ValueName: "CommboxMapper"; \
    ValueData: """{app}\{#AppExe}"" --minimized"; \
    Flags: uninsdeletevalue; Tasks: startupentry

[Run]
; Launch the app after install
Filename: "{app}\{#AppExe}"; Parameters: ""; \
    Description: "Launch {#AppName}"; Flags: postinstall nowait skipifsilent

[UninstallRun]
; Make sure the process is stopped before uninstalling
Filename: "powershell.exe"; \
    Parameters: "-Command ""Stop-Process -Name CommboxMapper -Force -ErrorAction SilentlyContinue"""; \
    RunOnceId: "StopProcess"; Flags: runhidden

[UninstallDelete]
; Clean up the settings file left next to the EXE
Type: files; Name: "{app}\CommboxMapper.json"
Type: dirifempty; Name: "{app}"
