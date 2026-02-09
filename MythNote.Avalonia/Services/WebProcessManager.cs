using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MythNote.Avalonia.Services;

public class WebProcessManager : IDisposable
{
    private Process? _webProcess;
    private readonly string _executablePath;
    private int _currentPort = 5000;
    private TaskCompletionSource<bool>? _startTcs;
    private string? _lastErrorMessage;

    public event Action<int>? OnPortFound;
    public event Action<string>? OnOutput;
    
    public int WebPort => _currentPort;
    public string? LastErrorMessage => _lastErrorMessage;

    public WebProcessManager()
    {
        bool isOsx = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        string fileName = isOsx ? "MythNote.Web" : "MythNote.Web.exe";

        string baseDir = AppDomain.CurrentDomain.BaseDirectory;

        if (isOsx && baseDir.Contains(".app/Contents/MacOS"))
        {
            // 核心修复：从 MacOS 目录向上查找，定位到 Resources 目录
            // 结构：MythNote.app/Contents/Resources/MythNote.Web
            _executablePath = Path.GetFullPath(Path.Combine(baseDir, "..", "Resources", fileName));
        }
        else
        {
            // Windows 或开发环境
            _executablePath = Path.Combine(baseDir, fileName);
        }
    }

    public bool IsRunning => _webProcess != null && !_webProcess.HasExited;

    public async Task<bool> StartAsync(CancellationToken ct = default)
    {
        if (IsRunning) return true;

        if (!File.Exists(_executablePath))
        {
            _lastErrorMessage = $"未找到 Web 组件二进制文件，请确保 {Path.GetFileName(_executablePath)} 在程序目录下。";
            OnOutput?.Invoke($"错误：{_lastErrorMessage}");
            return false;
        }

        _startTcs = new TaskCompletionSource<bool>();
        _currentPort = FindAvailablePort(5000);

        var startInfo = new ProcessStartInfo
        {
            // 关键：不再通过 dotnet 启动，直接运行二进制文件
            FileName = _executablePath,
            Arguments = $"--urls \"http://localhost:{_currentPort}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(_executablePath) ?? string.Empty
        };

        _webProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        _webProcess.OutputDataReceived += (s, e) => HandleOutput(e.Data);
        _webProcess.ErrorDataReceived += (s, e) => HandleOutput(e.Data);
        _webProcess.Exited += (s, e) => HandleProcessExit();

        try
        {
            if (!_webProcess.Start()) return false;

            _webProcess.BeginOutputReadLine();
            _webProcess.BeginErrorReadLine();

            using (ct.Register(() => _startTcs.TrySetCanceled()))
            {
                var timeoutTask = Task.Delay(15000, ct);
                if (await Task.WhenAny(_startTcs.Task, timeoutTask) == timeoutTask)
                {
                    Stop();
                    _lastErrorMessage = "Web 二进制服务启动超时，未能及时就绪";
                    OnOutput?.Invoke($"启动超时：{_lastErrorMessage}");
                    return false;
                }
            }
            return await _startTcs.Task;
        }
        catch (Exception ex)
        {
            _lastErrorMessage = $"二进制进程启动异常: {ex.Message}";
            OnOutput?.Invoke($"错误：{_lastErrorMessage}");
            return false;
        }
    }

    private void HandleOutput(string? data)
    {
        if (string.IsNullOrEmpty(data)) return;
        OnOutput?.Invoke(data);

        // 检查是否为错误信息
        if (data.Contains("error", StringComparison.OrdinalIgnoreCase) || 
            data.Contains("exception", StringComparison.OrdinalIgnoreCase) ||
            data.Contains("failed", StringComparison.OrdinalIgnoreCase) ||
            data.Contains("错误", StringComparison.OrdinalIgnoreCase))
        {
            _lastErrorMessage = data;
        }

        // 匹配 ASP.NET Core 启动成功标志
        if (data.Contains("Application started") || data.Contains("Now listening on"))
        {
            _startTcs?.TrySetResult(true);
        }

        var match = Regex.Match(data, @"localhost:(\d+)");
        if (match.Success && int.TryParse(match.Groups[1].Value, out var port))
        {
            _currentPort = port;
            OnPortFound?.Invoke(port);
        }
    }

    private void HandleProcessExit()
    {
        if (_webProcess != null && _webProcess.ExitCode != 0)
        {
            _lastErrorMessage = $"Web进程异常退出，退出代码: {_webProcess.ExitCode}";
            OnOutput?.Invoke($"错误：{_lastErrorMessage}");
            _startTcs?.TrySetResult(false);
        }
    }

    private int FindAvailablePort(int startPort)
    {
        for (int port = startPort; port < startPort + 100; port++)
        {
            try
            {
                using var listener = new TcpListener(IPAddress.Loopback, port);
                listener.Start();
                return port;
            }
            catch { continue; }
        }
        return startPort;
    }

    public void Stop()
    {
        if (_webProcess == null) return;
        try
        {
            if (!_webProcess.HasExited)
            {
                _webProcess.Kill(true); // 杀死整个进程树
                _webProcess.WaitForExit(2000);
            }
        }
        finally
        {
            _webProcess.Dispose();
            _webProcess = null;
        }
    }

    public void Dispose() => Stop();
}