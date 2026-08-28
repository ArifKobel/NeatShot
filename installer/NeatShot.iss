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
WizardSizePercent=110
WizardImageFile=assets\wizard-*.bmp
WizardSmallImageFile=assets\wizard-small-*.bmp
WizardImageStretch=no
DisableWelcomePage=no
DisableDirPage=auto
DisableReadyPage=yes
ShowLanguageDialog=no
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Messages]
WelcomeLabel1=Welcome to NeatShot
WelcomeLabel2=NeatShot lives in your tray and captures the screen, a window or a region with one shortcut. Click Install to continue.
FinishedHeadingLabel=You're all set
FinishedLabel=Alt+Shift+1 captures the screen, Alt+Shift+2 a window and Alt+Shift+3 a region. Each capture lands in a card at the bottom left, ready to copy, save or annotate.

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
