; Inno Setup Script for BabyKeyBoardSmash
; Defines non-administrative (per-user) setup configuration for CI/CD

#define MyAppName "BabyKeyBoardSmash by Mahfoz"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Mahfoz"
#define MyAppExeName "BabyKeyBoardSmash.exe"

[Setup]
AppId={{D8C04F09-8B39-44F4-98F1-57FE46853CD7}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\BabyKeyBoardSmashByMahfoz
DefaultGroupName=BabyKeyBoardSmashByMahfoz
DisableProgramGroupPage=yes
OutputDir=..\dist
OutputBaseFilename=BabyKeyBoardSmash_InnoSetup
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\dist\app\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
