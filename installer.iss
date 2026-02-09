[Setup]
; AppId 是程序的唯一标识，建议点击 Tools -> Generate GUID 替换一个你自己的
AppId={{9F5A4E31-1B2C-4D3E-A5F6-7B8C9D0E1F2A}
AppName=MythNote
AppVersion=1.0.1
WizardStyle=modern
SetupIconFile=MythNote.Avalonia\Assets\icon.ico
DefaultDirName={autopf}\MythNote
DefaultGroupName=MythNote
UninstallDisplayIcon={app}\MythNote.Avalonia.exe
Compression=lzma2
SolidCompression=yes
OutputBaseFilename=MythNote-Windows-Setup
OutputDir=./setup-out

[Files]
; 源文件路径
Source: "MythNote.Avalonia/bin/Release/net8.0/win-x64/publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\MythNote"; Filename: "{app}\MythNote.Avalonia.exe"
Name: "{commondesktop}\MythNote"; Filename: "{app}\MythNote.Avalonia.exe"

[Run]
Description: "Launch MythNote"; Flags: nowait postinstall skipifsilent; Filename: "{app}\MythNote.Avalonia.exe"

[Dirs]
; 授予所有用户对安装目录的读写权限
Name: "{app}"; Permissions: users-full

[Code]
// ---------------------------------------------------------
// 卸载前的进程检查逻辑
// ---------------------------------------------------------
function InitializeUninstall(): Boolean;
var
  FSWbemLocator: Variant;
  FWMIService: Variant;
  FWbemObjectSet: Variant;
  ProcessName: string;
begin
  Result := True;
  ProcessName := 'MythNote.Avalonia.exe'; // 确保这里是任务管理器里的进程名

  try
    // 使用 WMI 查询进程状态
    FSWbemLocator := CreateOleObject('WbemScripting.SWbemLocator');
    FWMIService := FSWbemLocator.ConnectServer('', 'root\CIMV2');
    FWbemObjectSet := FWMIService.ExecQuery(
      Format('SELECT Name FROM Win32_Process WHERE Name = "%s"', [ProcessName])
    );

    // 如果进程存在，Count 会大于 0
    if FWbemObjectSet.Count > 0 then
    begin
      MsgBox('检测到 ' + ProcessName + ' 正在运行！' + #13#10#13#10 +
             '请先彻底关闭程序，然后再尝试卸载。', mbCriticalError, MB_OK);
      
      // 返回 False 代表终止卸载流程
      Result := False;
    end;
  except
    // 如果 WMI 异常，默认允许继续，防止因系统环境问题导致永远无法卸载
    Result := True;
  end;
end;