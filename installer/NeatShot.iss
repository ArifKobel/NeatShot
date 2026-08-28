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
WelcomeLabel2=NeatShot lives in your tray and captures the screen, a window or a region with one shortcut.
FinishedHeadingLabel=You're all set
ClickNext=
WizardSelectTasks=Options
SelectTasksDesc=A couple of things you can decide now.
SelectTasksLabel2=You can change this later in Settings.
WizardInstalling=Installing
InstallingLabel=Copying NeatShot to your computer.
ClickFinish=
FinishedLabel=Alt+Shift+1 captures the screen, Alt+Shift+2 a window and Alt+Shift+3 a region. Each capture lands in a card at the bottom left, ready to copy, save or annotate.

[Tasks]
Name: "autostart"; Description: "Launch NeatShot when I sign in"

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

[Code]
const
  Background = $191515;
  Panel = $221E1E;
  Foreground = $F7F5F5;
  Muted = $A39A9A;
  DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

function DwmSetWindowAttribute(Wnd: HWND; Attribute: Integer; var Value: Integer; Size: Integer): Integer;
  external 'DwmSetWindowAttribute@dwmapi.dll stdcall delayload';

function SetWindowTheme(Wnd: HWND; AppName: String; IdList: Integer): Integer;
  external 'SetWindowTheme@uxtheme.dll stdcall';

procedure Darken(Control: TWinControl);
begin
  SetWindowTheme(Control.Handle, 'DarkMode_Explorer', 0);
end;

procedure Ink(Text: TNewStaticText; Color: TColor);
begin
  Text.Font.Color := Color;
  Text.Color := Background;
end;

procedure InitializeWizard;
var
  Dark: Integer;
begin
  Dark := 1;
  DwmSetWindowAttribute(WizardForm.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, Dark, SizeOf(Dark));

  WizardForm.Color := Background;
  WizardForm.MainPanel.Color := Panel;
  WizardForm.InnerPage.Color := Background;
  WizardForm.WelcomePage.Color := Background;
  WizardForm.FinishedPage.Color := Background;
  WizardForm.Bevel.Visible := False;
  WizardForm.WizardSmallBitmapImage.BackColor := Panel;
  WizardForm.WizardBitmapImage.BackColor := Background;
  WizardForm.WizardBitmapImage2.BackColor := Background;
  WizardForm.Bevel1.Visible := False;

  Ink(WizardForm.WelcomeLabel1, Foreground);
  Ink(WizardForm.WelcomeLabel2, Muted);
  Ink(WizardForm.FinishedHeadingLabel, Foreground);
  Ink(WizardForm.FinishedLabel, Muted);
  WizardForm.PageNameLabel.Font.Color := Foreground;
  WizardForm.PageNameLabel.Color := Panel;
  WizardForm.PageDescriptionLabel.Font.Color := Muted;
  WizardForm.PageDescriptionLabel.Color := Panel;
  Ink(WizardForm.SelectDirLabel, Muted);
  Ink(WizardForm.SelectDirBrowseLabel, Muted);
  Ink(WizardForm.DiskSpaceLabel, Muted);
  Ink(WizardForm.SelectTasksLabel, Muted);
  Ink(WizardForm.StatusLabel, Muted);
  Ink(WizardForm.FilenameLabel, Muted);

  WizardForm.DirEdit.Color := Panel;
  WizardForm.DirEdit.Font.Color := Foreground;
  WizardForm.TasksList.Color := Background;
  WizardForm.TasksList.Font.Color := Foreground;
  WizardForm.RunList.Color := Background;
  WizardForm.RunList.Font.Color := Foreground;

  Darken(WizardForm.DirEdit);
  Darken(WizardForm.DirBrowseButton);
  Darken(WizardForm.TasksList);
  Darken(WizardForm.RunList);
  Darken(WizardForm.ProgressGauge);
  Darken(WizardForm.BackButton);
  Darken(WizardForm.NextButton);
  Darken(WizardForm.CancelButton);
end;
