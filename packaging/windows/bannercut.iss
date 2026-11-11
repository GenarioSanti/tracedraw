[Setup]
AppId={{DA75436F-83C3-4FE1-8D3B-EA5275602357}
AppName=TraceDraw com BannerCut
AppVersion=0.44.0-bannercut-rc7
AppPublisher=SS Print
AppPublisherURL=https://github.com/GenarioSanti/tracedraw
DefaultDirName={localappdata}\Programs\TraceDraw-BannerCut
DefaultGroupName=TraceDraw com BannerCut
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\..\dist
OutputBaseFilename=TraceDraw-BannerCut-Setup
SetupIconFile=..\..\assets\icon\tracedraw.ico
UninstallDisplayIcon={app}\tracedraw.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
[Files]
Source: "..\..\target\release\tracedraw.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\integrations\bannercut\BannerCutIntegrado.exe"; DestDir: "{app}\integration"; Flags: ignoreversion
Source: "..\..\integrations\bannercut\LEIA-ME.txt"; DestDir: "{app}\integration"; Flags: ignoreversion
Source: "..\..\LICENSE-MIT"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\LICENSE-APACHE"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\ATTRIBUTION.md"; DestDir: "{app}"; Flags: ignoreversion
[Icons]
Name: "{group}\TraceDraw com BannerCut"; Filename: "{app}\tracedraw.exe"
Name: "{group}\Configurar integracao"; Filename: "{app}\integration\BannerCutIntegrado.exe"; Parameters: "--install-bundled ""{app}\tracedraw.exe"""
Name: "{group}\Desinstalar TraceDraw com BannerCut"; Filename: "{uninstallexe}"
[Run]
Filename: "{app}\integration\BannerCutIntegrado.exe"; Parameters: "--install-bundled ""{app}\tracedraw.exe"""; StatusMsg: "Configurando os botoes e a integracao de corte..."; Flags: waituntilterminated
