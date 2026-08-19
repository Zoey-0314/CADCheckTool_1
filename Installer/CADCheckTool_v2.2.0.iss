#define MyAppName "CADCheckTool"
#define MyAppVersion "2.2.0"
#define MyAppPublisher "Zoey-0314"
#define MyAppURL "https://github.com/Zoey-0314/CADCheckTool_1"
#define MyAppId "{{DE764F27-CF1C-420B-918A-D2F5DC66807C}"

[Setup]
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={code:GetDefaultDirName}
DisableDirPage=yes
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog commandline
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\artifacts\release
OutputBaseFilename=CADCheckTool_1_Setup_v2.2.0
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
SetupLogging=yes
CloseApplications=no
RestartApplications=no
UninstallDisplayName={#MyAppName} {#MyAppVersion}
UninstallDisplayIcon={app}\Contents\Windows\CADCheckTool_1.dll
VersionInfoVersion=2.2.0.0
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription=CADCheckTool AutoCAD 2024 plugin installer
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}
MinVersion=10.0.17763

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[InstallDelete]
Type: filesandordirs; Name: "{app}\Contents"
Type: filesandordirs; Name: "{app}\Docs"
Type: files; Name: "{app}\PackageContents.xml"

[Files]
Source: "..\artifacts\bundle\CADCheckTool.bundle\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Code]
const
  DotNet48Release = 528040;

function GetDefaultDirName(Param: String): String;
begin
  if IsAdminInstallMode then
  begin
    Result := ExpandConstant(
      '{autopf}\Autodesk\ApplicationPlugins\CADCheckTool.bundle');
  end
  else
  begin
    Result := ExpandConstant(
      '{userappdata}\Autodesk\ApplicationPlugins\CADCheckTool.bundle');
  end;
end;

function GetInstallModeName: String;
begin
  if IsAdminInstallMode then
    Result := '所有用户'
  else
    Result := '当前用户';
end;

function IsDotNet48Installed: Boolean;
var
  ReleaseValue: Cardinal;
begin
  Result :=
    RegQueryDWordValue(
      HKLM64,
      'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full',
      'Release',
      ReleaseValue) and
    (ReleaseValue >= DotNet48Release);
end;

function IsAutoCADRunning: Boolean;
var
  ResultCode: Integer;
begin
  Result :=
    Exec(
      ExpandConstant('{cmd}'),
      '/C tasklist /FI "IMAGENAME eq acad.exe" | find /I "acad.exe" >nul',
      '',
      SW_HIDE,
      ewWaitUntilTerminated,
      ResultCode) and
    (ResultCode = 0);
end;

function InitializeSetup: Boolean;
begin
  Result := True;

  if not IsDotNet48Installed then
  begin
    MsgBox(
      '未检测到 .NET Framework 4.8。请先安装 .NET Framework 4.8，再重新运行安装程序。',
      mbError,
      MB_OK);
    Result := False;
  end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := '';

  if IsAutoCADRunning then
  begin
    Result :=
      '检测到 AutoCAD 正在运行。请保存图纸并完全关闭 AutoCAD，然后重新点击“安装”。';
  end;
end;

procedure RemoveLegacyRegistrationRecursive(
  RootKey: Integer;
  const KeyPath: String);
var
  SubKeyNames: TArrayOfString;
  Index: Integer;
  ChildPath: String;
begin
  if CompareText(ExtractFileName(KeyPath), 'Applications') = 0 then
  begin
    RegDeleteKeyIncludingSubkeys(
      RootKey,
      KeyPath + '\CADCheckTool_1');
  end;

  if RegGetSubkeyNames(RootKey, KeyPath, SubKeyNames) then
  begin
    for Index := 0 to GetArrayLength(SubKeyNames) - 1 do
    begin
      ChildPath := KeyPath + '\' + SubKeyNames[Index];
      RemoveLegacyRegistrationRecursive(RootKey, ChildPath);
    end;
  end;
end;

procedure RemoveLegacyRegistrations;
begin
  RemoveLegacyRegistrationRecursive(
    HKCU64,
    'SOFTWARE\Autodesk\AutoCAD');

  RemoveLegacyRegistrationRecursive(
    HKCU32,
    'SOFTWARE\Autodesk\AutoCAD');

  if IsAdminInstallMode then
  begin
    RemoveLegacyRegistrationRecursive(
      HKLM64,
      'SOFTWARE\Autodesk\AutoCAD');

    RemoveLegacyRegistrationRecursive(
      HKLM32,
      'SOFTWARE\Autodesk\AutoCAD');
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssInstall then
  begin
    RemoveLegacyRegistrations;
  end;

  if CurStep = ssPostInstall then
  begin
    MsgBox(
      'CADCheckTool v2.2.0 已为' + GetInstallModeName + '安装完成。' +
      #13#10 + #13#10 +
      '安装位置：' + ExpandConstant('{app}') + #13#10 +
      '请重新启动 AutoCAD 2024。插件会自动加载，无需 NETLOAD，也无需手动修改注册表。' +
      #13#10 + '进入 AutoCAD 后输入 CHECKDRAWING 即可使用。',
      mbInformation,
      MB_OK);
  end;
end;
