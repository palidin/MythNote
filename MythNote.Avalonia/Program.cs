using Avalonia;
using System;
using System.Threading;

namespace MythNote.Avalonia;

class Program
{
    private static readonly string MutexName = "MythNote_Avalonia_Singleton_Mutex";
    private static Mutex? _mutex;

    [STAThread]
    public static void Main(string[] args)
    {
        // 尝试创建或打开互斥体
        _mutex = new Mutex(true, MutexName, out bool isNewInstance);

        if (!isNewInstance)
        {
            // 如果互斥体已存在，说明已有实例在运行
            Console.WriteLine("MythNote已在运行中，退出当前实例。");
            return;
        }

        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            // 释放互斥体
            _mutex.ReleaseMutex();
            _mutex.Dispose();
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            // .With(new MacOSPlatformOptions { ShowInDock = true })
            // .With(new SkiaOptions { web = false }) // macOS 禁用 WebGL 避免兼容问题
            .WithInterFont()
            .LogToTrace();
}
