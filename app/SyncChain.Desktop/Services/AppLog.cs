using System.Diagnostics;

namespace SyncChain.Desktop.Services;

// Logger nhẹ, đồng nhất cho toàn bộ Desktop. In ra cả Debug output (Visual Studio)
// lẫn stdout của tiến trình (thấy được khi chạy bằng script run-frontend.ps1).
// Định dạng thống nhất: [HH:mm:ss] [Desktop/<Scope>] <message>.
public static class AppLog
{
    public static void Info(string scope, string message) => Write("INFO", scope, message);
    public static void Warn(string scope, string message) => Write("WARN", scope, message);
    public static void Error(string scope, string message, Exception? ex = null)
        => Write("ERROR", scope, ex is null ? message : $"{message} :: {ex.GetType().Name}: {ex.Message}");

    private static void Write(string level, string scope, string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] [Desktop/{scope}] {level}: {message}";
        Debug.WriteLine(line);
        Console.WriteLine(line);
    }
}
