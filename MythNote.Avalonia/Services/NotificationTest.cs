namespace MythNote.Avalonia.Services;

public static class NotificationTest
{
    public static void TestErrorNotification()
    {
        NotificationService.ShowError("测试错误", "这是一个测试错误消息，用于验证系统通知功能是否正常工作。");
    }

    public static void TestErrorNotificationWithExit()
    {
        NotificationService.ShowError("测试错误并退出", "这是一个测试错误消息，显示后将退出程序。", exitAfterShow: true);
    }

    public static void TestWarningNotification()
    {
        NotificationService.ShowWarning("测试警告", "这是一个测试警告消息。");
    }

    public static void TestInfoNotification()
    {
        NotificationService.ShowInfo("测试信息", "这是一个测试信息消息。");
    }
}