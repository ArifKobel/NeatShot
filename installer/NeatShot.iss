#ifndef AppVersion
  #define AppVersion "0.1.0"
#endif
#ifndef PublishDir
  #define PublishDir "..\artifacts\publish"
#endif
#ifndef OutputDir
  #define OutputDir "..\artifacts"
#endif

[Setup]
AppId={{8C1B2D0E-6F0A-4B7E-9B8C-3D2F1E0A5C71}
AppName=NeatShot
AppVersion={#AppVersion}
AppVerName=NeatShot {#AppVersion}
AppPublisher=Arif Kobel
AppPublisherURL=https://github.com/ArifKobel/NeatShot
AppSupportURL=https://github.com/ArifKobel/NeatShot/issues
DefaultDirName={autopf}\NeatShot
DefaultGroupName=NeatShot
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir={#OutputDir}
OutputBaseFilename=NeatShot-{#AppVersion}-Setup
SetupIconFile={#PublishDir}\Assets\neatshot.ico
UninstallDisplayIcon={app}\NeatShot.exe
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
RestartApplications=no
LicenseFile=..\LICENSE

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "autostart"; Description: "Launch NeatShot when I sign in"; GroupDescription: "Startup:"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\NeatShot"; Filename: "{app}\NeatShot.exe"
Name: "{group}\Uninstall NeatShot"; Filename: "{uninstallexe}"

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "NeatShot"; ValueData: """{app}\NeatShot.exe"""; Flags: uninsdeletevalue; Tasks: autostart

[Run]
Filename: "{app}\NeatShot.exe"; Description: "Launch NeatShot"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "taskkill"; Parameters: "/IM NeatShot.exe /F"; Flags: runhidden; RunOnceId: "StopNeatShot"

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\NeatShot\Cache"
