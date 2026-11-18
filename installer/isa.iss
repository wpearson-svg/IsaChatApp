; isa.iss -- Inno Setup 6 script for the Isa demo installer (Willis, November 17, 2026).
; FOR DEMONSTRATIONS, NOT A RELEASE. Built by build-package.ps1 (it runs ISCC when Inno Setup 6 is installed),
; from the files it puts in out\Isa. Installs for the current user only (no administrator rights needed),
; into %LOCALAPPDATA%\Isa-Demo, because Isa writes her databases and logs next to herself.

#define AppName "Isa (demo)"
#define AppVersion "2026.11.17"

[Setup]
AppId={{6F1C2B7E-3A4D-4E5F-9B21-1D2D5C07649D}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=Second Star Collective
DefaultDirName={localappdata}\Isa-Demo
DefaultGroupName=Isa (demo)
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputBaseFilename=Isa-Setup
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
UninstallDisplayName={#AppName}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Files]
Source: "out\Isa\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{group}\Start Isa"; Filename: "powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\scripts\start-isa.ps1"""; WorkingDir: "{app}"
Name: "{group}\Stop Isa"; Filename: "powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\scripts\stop-isa.ps1"""; WorkingDir: "{app}"
Name: "{group}\Set up Isa (first run)"; Filename: "powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\scripts\first-run.ps1"""; WorkingDir: "{app}"
Name: "{group}\Isa logs"; Filename: "{app}\logs"
Name: "{group}\Read me"; Filename: "{app}\README.md"
Name: "{group}\Uninstall Isa"; Filename: "{uninstallexe}"
Name: "{userdesktop}\Start Isa"; Filename: "powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\scripts\start-isa.ps1"""; WorkingDir: "{app}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Put ""Start Isa"" on the desktop"

[Dirs]
Name: "{app}\logs"

[Run]
Filename: "powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\scripts\first-run.ps1"""; WorkingDir: "{app}"; Description: "Set up Isa's services now (needs Docker Desktop running and the internet; several GB to download)"; Flags: postinstall nowait skipifsilent

[UninstallRun]
Filename: "powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\scripts\stop-isa.ps1"" -Remove"; RunOnceId: "StopIsa"; Flags: runhidden waituntilterminated

[UninstallDelete]
Type: filesandordirs; Name: "{app}\logs"
Type: filesandordirs; Name: "{app}\app\data"
Type: files; Name: "{app}\first-run.done"
Type: files; Name: "{app}\isa-app.pid"

[Code]
function InitializeSetup(): Boolean;
begin
  Result := True;
  if not FileExists(ExpandConstant('{commonpf64}\Docker\Docker\Docker Desktop.exe')) then
    if MsgBox('Isa needs Docker Desktop, and it does not seem to be installed.' + #13#10 + #13#10 +
              'You can install Isa now and Docker Desktop afterwards, then use "Set up Isa (first run)".' + #13#10 + #13#10 +
              'Continue?', mbConfirmation, MB_YESNO) = IDNO then
      Result := False;
end;
