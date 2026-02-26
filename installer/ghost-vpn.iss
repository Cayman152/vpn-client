#ifndef MyAppVersion
  #define MyAppVersion "0.1.0"
#endif

#ifndef SourceDir
  #define SourceDir "."
#endif

#ifndef OutputDir
  #define OutputDir "."
#endif

#define MyAppName "Ghost VPN"
#define MyAppPublisher "Ghost VPN"
#define MyAppURL "https://github.com/Cayman152/vpn-client"
#define MyAppExeName "GhostVPN.exe"
#define MyAppId "{{DCE5F312-9A08-4C22-8EA8-59FA62F8EF9E}}"

[Setup]
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\Ghost VPN
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir={#OutputDir}
OutputBaseFilename=GhostVPN-Setup-x64
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[InstallDelete]
Type: files; Name: "{app}\v2rayN.exe"
Type: files; Name: "{app}\V2RayN.exe"
Type: files; Name: "{autodesktop}\v2rayN.lnk"
Type: files; Name: "{autodesktop}\V2RayN.lnk"
Type: files; Name: "{userprograms}\v2rayN.lnk"
Type: files; Name: "{userprograms}\V2RayN.lnk"
Type: files; Name: "{commonprograms}\v2rayN.lnk"
Type: files; Name: "{commonprograms}\V2RayN.lnk"
Type: filesandordirs; Name: "{userprograms}\v2rayN"
Type: filesandordirs; Name: "{userprograms}\V2RayN"
Type: filesandordirs; Name: "{commonprograms}\v2rayN"
Type: filesandordirs; Name: "{commonprograms}\V2RayN"

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: files; Name: "{autodesktop}\v2rayN.lnk"
Type: files; Name: "{autodesktop}\V2RayN.lnk"
Type: files; Name: "{userprograms}\v2rayN.lnk"
Type: files; Name: "{userprograms}\V2RayN.lnk"
Type: files; Name: "{commonprograms}\v2rayN.lnk"
Type: files; Name: "{commonprograms}\V2RayN.lnk"
Type: filesandordirs; Name: "{userprograms}\v2rayN"
Type: filesandordirs; Name: "{userprograms}\V2RayN"
Type: filesandordirs; Name: "{commonprograms}\v2rayN"
Type: filesandordirs; Name: "{commonprograms}\V2RayN"
