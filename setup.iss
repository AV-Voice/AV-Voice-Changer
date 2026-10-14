; ═══════════════════════════════════════════════════════════════
; VybeShift v1.0 — Inno Setup 6 Installer Script
;
; Features:
;   • Background music (Digital_Genesis.mp3) with on/off toggle
;   • VybeShift logo in wizard
;   • Auto-installs VB-Cable if not present
;   • Self-contained app (no .NET required for users)
;   • Developer: Rehan Bandara — r2-vision.firebaseapp.com
; ═══════════════════════════════════════════════════════════════

#define AppName      "VybeShift"
#define AppVersion   "0.0.1"
#define AppPublisher "Rehan Bandara"
#define AppURL       "https://r2-vision.firebaseapp.com/"
#define AppCoffee    "https://buymeacoffee.com/rehan1104"
#define AppExeName   "VybeShift.exe"
#define BuildDir     "..\VybeShift.UI\bin\publish"

; ── App Metadata ──────────────────────────────────────────────
[Setup]
AppId                        ={{C7A3F21C-44D9-4E72-9C1B-56E8AB210044}
AppName                      ={#AppName}
AppVersion                   ={#AppVersion}
AppPublisher                 ={#AppPublisher}
AppPublisherURL              ={#AppURL}
AppSupportURL                ={#AppURL}
AppCopyright                 =© 2025 Rehan Bandara
AppVerName                   ={#AppName} {#AppVersion}
DefaultDirName               ={autopf}\{#AppName}
DefaultGroupName             ={#AppName}
DisableProgramGroupPage      =yes
OutputDir                    =Output
OutputBaseFilename           =VybeShift_Setup_v{#AppVersion}
Compression                  =lzma2/ultra64
SolidCompression             =yes
WizardStyle                  =modern
; Logo used as wizard sidebar image (Inno Setup 6 supports PNG)
WizardImageFile              =assets\logo.png
WizardSmallImageFile         =assets\logo.png
WizardImageStretch           =yes
SetupIconFile                =assets\icon.ico
PrivilegesRequired           =admin
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion                   =10.0
UninstallDisplayName         ={#AppName}
UninstallDisplayIcon         ={app}\{#AppExeName}
LicenseFile                  =..\assets\license.txt

; ── Languages ─────────────────────────────────────────────────
[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

; ── Tasks (checkboxes shown to user) ──────────────────────────
[Tasks]
Name: "desktopicon"; Description: "Create a &Desktop shortcut"; GroupDescription: "Additional shortcuts:"
Name: "startupicon"; Description: "Start VybeShift automatically with &Windows"; GroupDescription: "Startup:"; Flags: unchecked

; ── Files to install ──────────────────────────────────────────
[Files]
; Main application (self-contained — includes .NET 8 runtime)
Source: "{#BuildDir}\*"; DestDir: "{app}"; \
  Flags: ignoreversion recursesubdirs createallsubdirs; \
  AfterInstall: AfterAppInstall

; Background music — extracted to temp, played during setup
Source: "assets\Digital_Genesis.mp3"; DestDir: "{tmp}"; \
  Flags: ignoreversion deleteafterinstall

; VB-Cable driver (only extracted if not already installed)
Source: "drivers\VBCABLE_Setup_x64.exe"; DestDir: "{tmp}"; \
  Flags: ignoreversion deleteafterinstall; \
  Check: NeedVBCable

; ── Shortcuts ─────────────────────────────────────────────────
[Icons]
Name: "{group}\{#AppName}";           Filename: "{app}\{#AppExeName}"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}";     Filename: "{app}\{#AppExeName}"; Tasks: desktopicon
Name: "{userstartup}\{#AppName}";     Filename: "{app}\{#AppExeName}"; Tasks: startupicon

; ── Run after install ─────────────────────────────────────────
[Run]
; Install VB-Cable silently if not present
Filename: "{tmp}\VBCABLE_Setup_x64.exe"; \
  Parameters: "/S"; \
  StatusMsg: "Installing VB-Cable Virtual Audio Driver..."; \
  Flags: runhidden waituntilterminated shellexec; \
  Check: NeedVBCable

; Finish page: open developer website (optional)
Filename: "{#AppURL}"; \
  Description: "Visit developer website (r2-vision.firebaseapp.com)"; \
  Flags: nowait postinstall skipifsilent shellexec

; Finish page: Buy Me a Coffee (optional, unchecked by default)
Filename: "{#AppCoffee}"; \
  Description: "☕ Support the developer (Buy Me a Coffee)"; \
  Flags: nowait postinstall skipifsilent shellexec unchecked

; Launch VybeShift
Filename: "{app}\{#AppExeName}"; \
  Description: "Launch {#AppName} now"; \
  Flags: nowait postinstall skipifsilent

; ── Cleanup on uninstall ──────────────────────────────────────
[UninstallDelete]
Type: filesandordirs; Name: "{userappdata}\VybeShift"

; ── Registry ──────────────────────────────────────────────────
[Registry]
Root: HKCU; \
  Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; \
  ValueType: string; ValueName: "VybeShift"; \
  ValueData: """{app}\{#AppExeName}"""; \
  Flags: uninsdeletevalue; \
  Tasks: startupicon

; ═══════════════════════════════════════════════════════════════
; [Code] — Custom installer Pascal logic
; ═══════════════════════════════════════════════════════════════
[Code]

// ── WinMM MCI for audio playback ──────────────────────────────
function mciSendString(lpstrCommand: AnsiString; lpstrReturn: AnsiString;
  uReturnLen: DWORD; hCallback: HWND): DWORD;
  external 'mciSendStringA@winmm.dll stdcall';

// ── Globals ───────────────────────────────────────────────────
var
  MusicEnabled:   Boolean;
  MusicToggleBtn: TButton;
  MusicPlaying:   Boolean;

// ── Check if VB-Cable is installed ───────────────────────────
function NeedVBCable: Boolean;
begin
  Result := not RegKeyExists(
    HKLM, 'SYSTEM\CurrentControlSet\Services\VBAudioVACMME'
  );
end;

// ── After app files installed ─────────────────────────────────
procedure AfterAppInstall;
begin
  if not DirExists(ExpandConstant('{userappdata}\VybeShift')) then
    CreateDir(ExpandConstant('{userappdata}\VybeShift'));
end;

// ── Play background music via MCI ────────────────────────────
procedure StartMusic;
var
  mp3Path: AnsiString;
begin
  if MusicPlaying then Exit;
  mp3Path := ExpandConstant('{tmp}\Digital_Genesis.mp3');
  mciSendString('open "' + mp3Path + '" type MPEGVideo alias VybeMusic', '', 0, 0);
  mciSendString('setaudio VybeMusic volume to 150', '', 0, 0); // 150 = 15% (max 1000)
  mciSendString('play VybeMusic repeat', '', 0, 0);
  MusicPlaying := True;
end;

procedure StopMusic;
begin
  if not MusicPlaying then Exit;
  mciSendString('stop VybeMusic', '', 0, 0);
  mciSendString('close VybeMusic', '', 0, 0);
  MusicPlaying := False;
end;

// ── Music toggle button click ─────────────────────────────────
procedure ToggleMusic(Sender: TObject);
begin
  MusicEnabled := not MusicEnabled;
  if MusicEnabled then
  begin
    StartMusic;
    MusicToggleBtn.Caption := '🔇 Stop Music';
  end
  else
  begin
    StopMusic;
    MusicToggleBtn.Caption := '🎵 Play Music';
  end;
end;

// ── Wizard: custom welcome text + music toggle button ─────────
procedure InitializeWizard;
begin
  // Welcome text
  WizardForm.WelcomeLabel2.Caption :=
    'Welcome to VybeShift v0.0.1!' + #13#10#13#10 +
    'VybeShift is a free, real-time voice changer for Discord, Zoom, OBS and any app.' + #13#10#13#10 +
    'This installer will:' + #13#10 +
    '  * Install VybeShift (no .NET required - its built in)' + #13#10 +
    '  * Install VB-Cable virtual microphone driver (if not present)' + #13#10 +
    '  * Create Start Menu and optional Desktop shortcut' + #13#10#13#10 +
    'Developed by Rehan Bandara' + #13#10 +
    'r2-vision.firebaseapp.com' + #13#10#13#10 +
    'Click Next to continue.';

  // Extract the MP3 immediately so it is available before main install
  ExtractTemporaryFile('Digital_Genesis.mp3');

  // Auto-start music right away
  MusicEnabled := True;
  MusicPlaying := False;
  StartMusic;

  // Music toggle button globally on the wizard form (bottom left)
  MusicToggleBtn        := TButton.Create(WizardForm);
  MusicToggleBtn.Parent := WizardForm;
  MusicToggleBtn.Caption := '🔇 Stop Music';
  MusicToggleBtn.Width  := 100;
  MusicToggleBtn.Height := WizardForm.CancelButton.Height;
  MusicToggleBtn.Left   := 20;
  MusicToggleBtn.Top    := WizardForm.CancelButton.Top;
  MusicToggleBtn.OnClick := @ToggleMusic;
end;

// ── Auto-start music when welcome page appears ────────────────
procedure CurPageChanged(CurPageID: Integer);
begin
  if (CurPageID = wpWelcome) and (not MusicPlaying) and MusicEnabled then
  begin
    StartMusic;
    if MusicToggleBtn <> nil then
      MusicToggleBtn.Caption := '🔇 Stop Music';
  end;
end;

// ── Stop music on install complete or cancel ──────────────────
procedure DeinitializeSetup;
begin
  StopMusic;
end;

// ── On installer start: check prereqs ────────────────────────
function InitializeSetup: Boolean;
begin
  Result := True;
  if NeedVBCable and not FileExists(ExpandConstant('{tmp}\VBCABLE_Setup_x64.exe')) then
  begin
    if MsgBox(
      'VybeShift requires VB-Cable (free virtual audio driver).' + #13#10#13#10 +
      'It was not bundled with this installer.' + #13#10 +
      'After setup, download it from: https://vb-audio.com/Cable/' + #13#10#13#10 +
      'Continue installing VybeShift anyway?',
      mbConfirmation, MB_YESNO
    ) = IDNO then
      Result := False;
  end;
end;

// ── On uninstall: ask about VB-Cable ─────────────────────────
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  ResultCode: Integer;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    if MsgBox(
      'Do you also want to remove VB-Cable virtual audio driver?' + #13#10 +
      '(Only uninstall if no other apps use it)',
      mbConfirmation, MB_YESNO
    ) = IDYES then
      ShellExec('open', 'cmd.exe',
        '/C "' + ExpandConstant('{pf}') + '\VB\VBCABLE Driver Pack45\VBCABLE_Setup_x64.exe" /U',
        '', SW_HIDE, ewNoWait, ResultCode);
  end;
end;
