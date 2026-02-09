using System;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;

namespace MythNote.Avalonia.Services;

public interface ITrayIconService
{
    event Action? OnOpenRequested;
    event Action? OnQuitRequested;
    void Initialize();
    void UpdateStatus(string status);
    void UpdateToolTip(string toolTip);
}

public class TrayIconService : ITrayIconService, IDisposable
{
    public event Action? OnOpenRequested;
    public event Action? OnQuitRequested;

    private TrayIcon? _trayIcon;

    /// <summary>
    /// 初始化托盘图标
    /// </summary>
    public void Initialize()
    {
        // 防止重复初始化导致的句柄冲突
        if (_trayIcon != null) return;

        try
        {
            _trayIcon = new TrayIcon
            {
                Icon = LoadIcon(),
                ToolTipText = "MythNote - 笔记管理应用",
                // 关键点：初始化时创建一次静态菜单，之后不再更换 Menu 对象实例
                Menu = CreateStaticMenu(),
                IsVisible = true
            };

            // 禁用默认点击行为，完全依赖菜单项
            _trayIcon.Clicked += (s, e) => { };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TrayIcon] 初始化失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 更新状态信息
    /// 去掉了修改 Menu 的逻辑，改为通过 ToolTip 反馈状态，彻底规避 macOS 崩溃
    /// </summary>
    public void UpdateStatus(string status)
    {
        UpdateToolTip($"MythNote: {status}");
    }

    /// <summary>
    /// 安全地更新悬浮提示
    /// </summary>
    public void UpdateToolTip(string toolTip)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_trayIcon != null)
            {
                _trayIcon.ToolTipText = toolTip;
            }
        }, DispatcherPriority.Background);
    }

    /// <summary>
    /// 创建固定的原生菜单结构
    /// </summary>
    private NativeMenu CreateStaticMenu()
    {
        var menu = new NativeMenu();

        // 1. 打开项
        var openItem = new NativeMenuItem("打开 MythNote");
        openItem.Click += (s, e) => OnOpenRequested?.Invoke();
        menu.Add(openItem);

        // 分割线
        menu.Add(new NativeMenuItemSeparator());

        // 2. 退出项
        var quitItem = new NativeMenuItem("退出");
        quitItem.Click += (s, e) => OnQuitRequested?.Invoke();
        menu.Add(quitItem);

        return menu;
    }

    /// <summary>
    /// 加载图标资源
    /// </summary>
    private WindowIcon? LoadIcon()
    {
        var icoAsset = AssetLoader.Open(new Uri("avares://MythNote.Avalonia/Assets/icon.ico"));
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            icoAsset = AssetLoader.Open(new Uri("avares://MythNote.Avalonia/Assets/tray-icon.png"));
        }

        return new WindowIcon(icoAsset);
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_trayIcon != null)
        {
            _trayIcon.IsVisible = false;
            _trayIcon.Dispose();
            _trayIcon = null;
        }
    }
}