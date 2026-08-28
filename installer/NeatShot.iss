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
DisableDirPage=yes
DisableReadyPage=yes
DisableFinishedPage=yes
ShowLanguageDialog=no
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Messages]
WelcomeLabel1=Welcome to NeatShot
WelcomeLabel2=NeatShot lives in your tray and captures the screen, a window or a region with one shortcut.%n%nAlt+Shift+1 for the screen, Alt+Shift+2 for a window, Alt+Shift+3 for a region.
ClickNext=
WizardInstalling=Installing
InstallingLabel=Copying NeatShot to your computer.

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\NeatShot"; Filename: "{app}\NeatShot.exe"
Name: "{group}\Uninstall NeatShot"; Filename: "{uninstallexe}"

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "NeatShot"; ValueData: """{app}\NeatShot.exe"""; Flags: uninsdeletevalue; Check: AutostartWanted

[Run]
Filename: "{app}\NeatShot.exe"; Flags: nowait

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

var
  AutostartBox: TNewCheckBox;

function AutostartWanted: Boolean;
begin
  Result := AutostartBox.Checked;
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = wpWelcome then
    WizardForm.NextButton.Caption := 'Install';
end;

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
  WizardForm.Bevel.Visible := False;
  WizardForm.WizardSmallBitmapImage.BackColor := Panel;
  WizardForm.WizardBitmapImage.BackColor := Background;
  WizardForm.WizardBitmapImage2.BackColor := Background;
  WizardForm.Bevel1.Visible := False;

  Ink(WizardForm.WelcomeLabel1, Foreground);
  Ink(WizardForm.WelcomeLabel2, Muted);

  AutostartBox := TNewCheckBox.Create(WizardForm);
  AutostartBox.Parent := WizardForm.WelcomePage;
  AutostartBox.Left := WizardForm.WelcomeLabel2.Left;
  AutostartBox.Top := WizardForm.WelcomePage.Height - ScaleY(40);
  AutostartBox.Width := WizardForm.WelcomeLabel2.Width;
  AutostartBox.Height := ScaleY(20);
  AutostartBox.Caption := 'Launch NeatShot when I sign in';
  AutostartBox.Checked := True;
  AutostartBox.Color := Background;
  AutostartBox.Font.Color := Foreground;
  Darken(AutostartBox);
  WizardForm.PageNameLabel.Font.Color := Foreground;
  WizardForm.PageNameLabel.Color := Panel;
  WizardForm.PageDescriptionLabel.Font.Color := Muted;
  WizardForm.PageDescriptionLabel.Color := Panel;
  Ink(WizardForm.StatusLabel, Muted);
  Ink(WizardForm.FilenameLabel, Muted);


  Darken(WizardForm.ProgressGauge);
  Darken(WizardForm.BackButton);
  Darken(WizardForm.NextButton);
  Darken(WizardForm.CancelButton);
end;
