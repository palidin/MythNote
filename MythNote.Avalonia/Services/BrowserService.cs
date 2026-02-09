using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MythNote.Avalonia.Services;

public interface IBrowserService
{
    bool OpenUrl(string url);
}

public class BrowserService : IBrowserService
{
    public bool OpenUrl(string url)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else
            {
                Console.WriteLine($"Unsupported platform for browser opening");
                return false;
            }

            Console.WriteLine($"Browser opened successfully: {url}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to open browser: {ex.Message}");
            return false;
        }
    }
}
