using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using MythNote.Avalonia.Services;
using MythNote.Avalonia.ViewModels;
using MythNote.Avalonia.Views;

namespace MythNote.Avalonia;

public partial class App : Application
{
    private TrayIconService? _trayService;
    private IBrowserService? _browserService;
    private WebProcessManager? _webProcessManager;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // 捕获 UI 线程未处理异常
        Dispatcher.UIThread.UnhandledException += (s, e) =>
        {
            File.WriteAllText(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log"),
                $"Unhandled Exception: {e.Exception}\n{e.Exception.StackTrace}"
            );


            NotificationService.ShowError("应用错误", $"Unhandled Exception: {e.Exception}\n{e.Exception.StackTrace}",
                exitAfterShow: true);
            e.Handled = true; // 防止进程立即终止
        };

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 1. 初始化服务
            _browserService = new BrowserService();
            _webProcessManager = new WebProcessManager();
            _trayService = new TrayIconService();

            // 绑定Web进程事件
            _webProcessManager.OnPortFound += OnPortFound;
            _webProcessManager.OnOutput += OnWebOutput;

            _trayService.Initialize();

            // 2. 绑定事件
            _trayService.OnOpenRequested += HandleOpenRequested;
            _trayService.OnQuitRequested += HandleQuitRequested;

            // 3. 创建并配置主窗口
            var mainWindow = new MainWindow
            {
                ShowInTaskbar = false,
                WindowState = WindowState.Minimized,
                IsVisible = false,
                Opacity = 0
            };
            desktop.MainWindow = mainWindow;

            // 拦截窗口关闭事件
            mainWindow.Closing += (s, e) =>
            {
                ((Window)s!).Hide();
                e.Cancel = true;
            };

            // 拦截窗口激活事件，确保窗口永远不会显示
            mainWindow.Activated += (s, e) => { ((Window)s!).Hide(); };

            // 启动Web服务器
            _ = StartWebServerAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task StartWebServerAsync()
    {
        if (_webProcessManager != null)
        {
            _trayService?.UpdateStatus("正在启动Web服务...");
            var success = await _webProcessManager.StartAsync();
            if (success)
            {
                HandleOpenRequested();
                _trayService?.UpdateStatus("Web服务运行中");
            }
            else
            {
                _trayService?.UpdateStatus("Web服务启动失败");

                // 显示系统错误通知并退出程序
                var errorMessage = _webProcessManager.LastErrorMessage ?? "Web服务启动失败，未知错误";
                NotificationService.ShowError("MythNote - Web服务启动失败", errorMessage, exitAfterShow: true);
            }
        }
    }

    private void OnPortFound(int port)
    {
        Console.WriteLine($"Web server started on port {port}");
        _trayService?.UpdateStatus($"Web服务运行中 - 端口:{port}");
        _trayService?.UpdateToolTip($"MythNote - Web服务运行在端口 {port}");
    }

    private void OnWebOutput(string output)
    {
        Console.WriteLine($"[Web] {output}");
    }

    private void HandleOpenRequested()
    {
        // 获取实际的Web服务地址
        if (_webProcessManager?.IsRunning == true)
        {
            var url = $"http://localhost:{_webProcessManager.WebPort}";
            _browserService?.OpenUrl(url);
        }
        else
        {
            _trayService?.UpdateStatus("Web服务未运行");
        }
    }

    private void HandleQuitRequested()
    {
        _webProcessManager?.Stop();
        _trayService?.Dispose();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}