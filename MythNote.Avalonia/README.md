# MythNote.Avalonia

MythNote 桌面客户端，基于 Avalonia UI 框架开发。

## 功能特性

- 系统托盘集成
- 自动启动和管理 Web 服务器
- 跨平台支持 (Windows, macOS, Linux)
- 现代化的用户界面

## 技术栈

- **Avalonia UI 11.2.1** - 跨平台 UI 框架
- **ReactiveUI 20.1.1** - MVVM 框架
- **.NET 8.0** - 运行时

## 项目结构

```
MythNote.Avalonia/
├── Services/              # 服务层
│   ├── TrayIconService.cs    # 系统托盘服务
│   └── WebProcessManager.cs  # Web 进程管理
├── ViewModels/             # 视图模型
│   ├── MainViewModel.cs      # 主窗口视图模型
│   └── ViewModelBase.cs      # 视图模型基类
├── Views/                  # 视图
│   ├── MainWindow.axaml      # 主窗口 XAML
│   └── MainWindow.axaml.cs   # 主窗口代码
├── Assets/                 # 资源文件
│   ├── icon.ico              # 应用图标
│   └── icon.png              # PNG 图标
├── App.axaml               # 应用程序 XAML
├── App.axaml.cs            # 应用程序代码
└── Program.cs              # 程序入口
```

## 运行方式

### 本地开发

1. 确保已安装 .NET 8.0 SDK
2. 构建项目：
   ```bash
   dotnet build
   ```
3. 运行应用：
   ```bash
   dotnet run
   ```

### 跨平台打包

#### 自动化打包 (GitHub Actions)

项目包含完整的 GitHub Actions 工作流，支持自动构建和打包：

- **Windows**: `win-x64` 可执行文件
- **macOS**: `osx-x64` 和 `osx-arm64` 应用程序包
- **Linux**: `linux-x64` 可执行文件

当推送标签（如 `v1.0.0`）时，会自动创建 GitHub Release 并上传所有平台的构建产物。

#### 本地打包

##### macOS 打包

在 macOS 系统上运行：

```bash
# 使用脚本打包（推荐）
./scripts/package-macos.sh

# 或手动打包
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true -o ./dist/osx-x64
dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -o ./dist/osx-arm64
```

生成的文件：
- `mythnote-macos-x64.zip` - Intel Mac 应用程序包
- `mythnote-macos-arm64.zip` - Apple Silicon Mac 应用程序包
- `mythnote-macos-universal.zip` - 通用二进制文件（如果支持）

##### Windows 打包

```powershell
# 使用 PowerShell 脚本
.\scripts\package-macos.ps1

# 或手动打包
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./dist/win-x64
```

##### Linux 打包

```bash
# 使用脚本
./scripts/build.sh

# 或手动打包
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -o ./dist/linux-x64
```

#### 跨平台构建

使用通用构建脚本：

```bash
# 在任何平台上运行
./scripts/build.sh
```

该脚本会自动检测平台并执行相应的构建流程。

### macOS 应用程序配置

macOS 应用程序包含以下配置：

- **Info.plist**: 应用程序元数据和权限配置
- **entitlements.plist**: 系统权限和沙盒配置
- **应用图标**: 自动复制到应用程序包中
- **系统托盘支持**: 配置为后台应用 (`LSUIElement = true`)

### 分发注意事项

1. **代码签名**: macOS 应用程序需要代码签名才能在系统上正常运行
2. **公证**: 分发前建议通过 Apple 的公证服务
3. **最低系统版本**: macOS 10.15 (Catalina) 或更高版本
4. **权限**: 应用程序需要网络访问权限来运行 Web 服务器

## 工作原理

1. 应用启动时会自动启动内嵌的 Web 服务器 (MythNote.Web)
2. Web 服务器启动成功后，会自动打开默认浏览器访问 Web 界面
3. 关闭主窗口时，应用会隐藏到系统托盘而不是完全退出
4. 通过系统托盘菜单可以重新打开主窗口或完全退出应用

## 注意事项

- 首次运行时需要确保 MythNote.Web.dll 文件存在
- 应用会自动寻找可用的端口启动 Web 服务器
- 默认端口范围：5000-5099