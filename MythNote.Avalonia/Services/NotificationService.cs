using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace MythNote.Avalonia.Services;

public static class NotificationService
{
    // Windows原生API
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

    private const uint MB_ICONERROR = 0x00000010;
    private const uint MB_ICONWARNING = 0x00000030;
    private const uint MB_ICONINFORMATION = 0x00000040;

    public static void ShowError(string title, string message, bool exitAfterShow = false)
    {
        ShowNativeMessageBox(title, message, NativeMessageBoxType.Error, exitAfterShow);
    }

    public static void ShowWarning(string title, string message)
    {
        ShowNativeMessageBox(title, message, NativeMessageBoxType.Warning);
    }

    public static void ShowInfo(string title, string message)
    {
        ShowNativeMessageBox(title, message, NativeMessageBoxType.Info);
    }

    private static void ShowNativeMessageBox(string title, string message, NativeMessageBoxType type, bool exitAfterShow = false)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows原生MessageBox
            uint iconType = type switch
            {
                NativeMessageBoxType.Error => MB_ICONERROR,
                NativeMessageBoxType.Warning => MB_ICONWARNING,
                NativeMessageBoxType.Info => MB_ICONINFORMATION,
                _ => 0
            };
            
            MessageBox(IntPtr.Zero, message, title, iconType);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Linux使用zenity或kdialog
            ShowLinuxMessageBox(title, message, type);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // macOS使用osascript
            ShowMacMessageBox(title, message, type);
        }

        if (exitAfterShow) Environment.Exit(1);
    }

    private static void ShowLinuxMessageBox(string title, string message, NativeMessageBoxType type)
    {
        string icon = type switch
        {
            NativeMessageBoxType.Error => "error",
            NativeMessageBoxType.Warning => "warning", 
            NativeMessageBoxType.Info => "info",
            _ => "info"
        };

        // 尝试使用zenity
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "zenity",
            Arguments = $"--{icon} --text=\"{message}\" --title=\"{title}\" --no-wrap",
            UseShellExecute = false
        };

        try
        {
            System.Diagnostics.Process.Start(psi)?.WaitForExit();
        }
        catch
        {
            // 如果zenity不可用，尝试kdialog
            psi.FileName = "kdialog";
            psi.Arguments = $"--{icon} \"{message}\" --title \"{title}\"";
            try
            {
                System.Diagnostics.Process.Start(psi)?.WaitForExit();
            }
            catch
            {
                // 如果都不可用，使用控制台输出
                Console.WriteLine($"{title}: {message}");
            }
        }
    }

    private static void ShowMacMessageBox(string title, string message, NativeMessageBoxType type)
    {
        string icon = type switch
        {
            NativeMessageBoxType.Error => "stop",
            NativeMessageBoxType.Warning => "caution",
            NativeMessageBoxType.Info => "note",
            _ => "note"
        };

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "osascript",
            Arguments = $"-e 'display dialog \"{message}\" with title \"{title}\" buttons \"OK\" default button 1 with icon {icon}'",
            UseShellExecute = false
        };

        try
        {
            System.Diagnostics.Process.Start(psi)?.WaitForExit();
        }
        catch
        {
            // 如果osascript不可用，使用控制台输出
            Console.WriteLine($"{title}: {message}");
        }
    }

    private enum NativeMessageBoxType
    {
        Error,
        Warning,
        Info
    }
}
