; HT ISO Creator - Windows installer (Inno Setup 6)
; Build with Tools\Build-Release.ps1, which passes the version and the staged release folder:
;   ISCC.exe /DAppVersion=1.0.1 /DSourceDir=<staged files> /DOutputDir=<output> installer\HTISOCreator.iss
;
; The AppId below must never change: Windows uses it to recognise upgrades and the uninstall entry.

#ifndef AppVersion
  #error AppVersion is not defined. Build with Tools\Build-Release.ps1.
#endif
#ifndef SourceDir
  #error SourceDir is not defined. Build with Tools\Build-Release.ps1.
#endif
#ifndef OutputDir
  #define OutputDir "."
#endif

#define AppName "HT ISO Creator"
#define AppPublisher "HTREASURE LTD"
#define AppExe "HTISOCreator.exe"

[Setup]
AppId={{3CAE52B4-4DB9-47FE-B86D-6EF5CEE9E1E2}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL=https://www.htreasure.com
AppCopyright=Copyright (C) 2026 {#AppPublisher}
VersionInfoVersion={#AppVersion}.0
VersionInfoProductVersion={#AppVersion}
VersionInfoCompany={#AppPublisher}
VersionInfoDescription={#AppName} Setup
VersionInfoProductName={#AppName}
; Installs for all users (administrator) by default; the first page lets the user choose
; "Install for me only", which needs no administrator rights. /CURRENTUSER selects it silently.
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog commandline
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
UsePreviousAppDir=yes
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\{#AppExe}
SetupIconFile=..\HTISOCreator\Resources\app.ico
WizardStyle=modern
OutputDir={#OutputDir}
OutputBaseFilename=HTISOCreator_Setup_v{#AppVersion}
Compression=lzma2/max
SolidCompression=yes
; Windows 10 or later.
MinVersion=10.0
ArchitecturesInstallIn64BitMode=x64compatible
; Closes a running HT ISO Creator (after asking) so files can be replaced during an upgrade.
CloseApplications=yes
RestartApplications=no
AppMutex=HTREASURE.HTISOCreator.Running

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#SourceDir}\{#AppExe}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\{#AppExe}.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\DiscUtils.Core.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\DiscUtils.Iso9660.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\DiscUtils.Streams.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\THIRD-PARTY-NOTICES.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\Documentation\*"; DestDir: "{app}\Documentation"; Flags: ignoreversion recursesubdirs createallsubdirs

[InstallDelete]
; Documentation is replaced as a whole on upgrade, so renamed documents do not linger.
Type: filesandordirs; Name: "{app}\Documentation"

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExe}"; Comment: "Create and verify data ISO images"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Comment: "Create and verify data ISO images"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

; Settings, history and logs live in %APPDATA% / %LOCALAPPDATA%\HTREASURE LTD\HT ISO Creator and are
; deliberately NOT removed on upgrade or uninstall, so a reinstall keeps the user's preferences.

[Code]
// .NET Framework 4.6.2 or later: "Release" value 394802 or higher (Microsoft documentation,
// "How to: Determine which .NET Framework versions are installed").
function IsDotNet462Installed(): Boolean;
var
  release: Cardinal;
begin
  Result := RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', release) and (release >= 394802);
end;

function InitializeSetup(): Boolean;
begin
  Result := IsDotNet462Installed();
  if not Result then
    SuppressibleMsgBox('HT ISO Creator needs Microsoft .NET Framework 4.6.2 or later, which was not found on this computer.' + #13#10#13#10 +
      'Install .NET Framework 4.8 from Microsoft (Windows Update or https://dotnet.microsoft.com/download/dotnet-framework), then run this setup again.',
      mbCriticalError, MB_OK, IDOK);
end;
