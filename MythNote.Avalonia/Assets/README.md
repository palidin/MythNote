# Assets 资源文件

此目录包含 MythNote.Avalonia 应用程序的资源文件。

## 文件说明

- `icon.ico` - Windows 系统托盘图标文件
- `icon.png` - 高分辨率 PNG 图标，用于应用图标
- `tray-icon.png` - macOS 专用托盘图标（16x16 或 32x32 像素）

## 图标规格

- **ICO 文件**: 支持多尺寸 (16x16, 32x32, 48x48, 256x256)
- **PNG 文件**: 512x512 像素，透明背景
- **托盘图标 PNG**: 16x16 或 32x32 像素，简洁设计，适合菜单栏显示

## 使用方式

图标通过 Avalonia 的资源系统加载：

```csharp
// 应用图标
var appAsset = AssetLoader.Open(new Uri("avares://MythNote.Avalonia/Assets/icon.png"));
return new WindowIcon(appAsset);

// 托盘图标（平台特定）
var trayAsset = AssetLoader.Open(new Uri(PlatformHelpers.GetTrayIconPath()));
return new WindowIcon(trayAsset);
```

## 平台差异

- **Windows**: 应用图标和托盘图标都使用 ICO 格式
- **macOS**: 应用图标使用 PNG，托盘图标使用专门的 tray-icon.png
- **Linux**: 使用 ICO 格式

## 注意事项

- 确保图标文件已正确设置为嵌入资源
- 如需更换图标，请保持相同的文件名以避免代码修改
- macOS 托盘图标应该简洁明了，在深色和浅色模式下都清晰可见