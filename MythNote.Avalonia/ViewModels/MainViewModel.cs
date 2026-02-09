using System;
using System.ComponentModel;
using ReactiveUI;

namespace MythNote.Avalonia.ViewModels;

public class MainViewModel : ViewModelBase
{
    private string _statusMessage = "MythNote 就绪";

    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public MainViewModel()
    {
        StatusMessage = "应用程序已启动，托盘图标已显示";
    }
}
