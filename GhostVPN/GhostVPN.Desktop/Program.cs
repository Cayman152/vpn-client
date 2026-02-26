using GhostVPN.Desktop.Common;
using System.Runtime.InteropServices;
using System.Text;

namespace GhostVPN.Desktop;

internal class Program
{
    public static EventWaitHandle ProgramStarted;
    private static readonly object _startupLogLock = new();

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            if (OnStartup(args, out var startupReason) == false)
            {
                WriteStartupEvent($"Startup aborted: {startupReason}");
                if (startupReason == "already-running")
                {
                    ShowStartupDialog("Ghost VPN уже запущен. Проверьте окно приложения или значок в панели задач.", 0x40);
                }
                else
                {
                    var logPath = GetStartupLogPath();
                    ShowStartupDialog($"Не удалось запустить Ghost VPN.{Environment.NewLine}Подробности: {logPath}", 0x10);
                }

                Environment.Exit(0);
                return;
            }

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            var logPath = WriteStartupEvent("Fatal startup exception", ex);
            ShowStartupDialog(
                $"Клиент завершился на старте.{Environment.NewLine}Ошибка: {ex.Message}{Environment.NewLine}Лог: {logPath}",
                0x10);
            Environment.Exit(1);
        }
    }

    internal static void ReportFatalStartupError(string stage, Exception ex)
    {
        var logPath = WriteStartupEvent($"Fatal startup error ({stage})", ex);
        ShowStartupDialog(
            $"Клиент завершился на старте ({stage}).{Environment.NewLine}Ошибка: {ex.Message}{Environment.NewLine}Лог: {logPath}",
            0x10);
    }

    private static bool OnStartup(string[]? args, out string reason)
    {
        reason = "unknown";

        try
        {
            if (Utils.IsWindows())
            {
                var exePathKey = Utils.GetMd5(Utils.GetExePath());
                var rebootas = (args ?? []).Any(t => t == Global.RebootAs);
                ProgramStarted = new EventWaitHandle(false, EventResetMode.AutoReset, exePathKey, out var bCreatedNew);
                if (!rebootas && !bCreatedNew)
                {
                    ProgramStarted.Set();
                    reason = "already-running";
                    return false;
                }
            }
            else
            {
                _ = new Mutex(true, "GhostVPN", out var bOnlyOneInstance);
                if (!bOnlyOneInstance)
                {
                    reason = "already-running";
                    return false;
                }
            }

            if (!AppManager.Instance.InitApp())
            {
                reason = "init-failed";
                return false;
            }

            reason = "ok";
            return true;
        }
        catch (Exception ex)
        {
            WriteStartupEvent("OnStartup exception", ex);
            reason = "exception";
            return false;
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        var builder = AppBuilder.Configure<App>()
           .UsePlatformDetect()
           //.WithInterFont()
           .WithFontByDefault()
           .LogToTrace()
           .UseReactiveUI();

        if (OperatingSystem.IsMacOS())
        {
            var showInDock = Design.IsDesignMode || AppManager.Instance.Config.UiItem.MacOSShowInDock;
            builder = builder.With(new MacOSPlatformOptions { ShowInDock = showInDock });
        }

        return builder;
    }

    private static string WriteStartupEvent(string text, Exception? ex = null)
    {
        var logPath = GetStartupLogPath();
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {text}");
            if (ex != null)
            {
                sb.AppendLine(ex.ToString());
            }

            lock (_startupLogLock)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
                File.AppendAllText(logPath, sb.ToString(), Encoding.UTF8);
            }
        }
        catch
        {
            // ignore
        }

        return logPath;
    }

    private static string GetStartupLogPath()
    {
        try
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "GhostVPN",
                "guiLogs",
                "startup-fatal.log");
        }
        catch
        {
            return Path.Combine(Path.GetTempPath(), "GhostVPN-startup-fatal.log");
        }
    }

    private static void ShowStartupDialog(string message, uint iconType)
    {
        if (!Utils.IsWindows())
        {
            return;
        }

        try
        {
            _ = MessageBoxW(IntPtr.Zero, message, Global.AppName, iconType);
        }
        catch
        {
            // ignore
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int MessageBoxW(IntPtr hWnd, string lpText, string lpCaption, uint uType);
}
