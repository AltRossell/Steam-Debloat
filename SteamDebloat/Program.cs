using Avalonia;
using System;

namespace SteamDebloat
{
    class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                BuildAvaloniaApp()
                    .StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                string logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "SteamDebloat_Crash.log");
                System.IO.File.WriteAllText(logPath, ex.ToString());
                // Also try to write to local dir in case temp is inaccessible
                try { System.IO.File.WriteAllText("crash.log", ex.ToString()); } catch { }
            }
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
