using System;
using System.IO;
using MoonWorks;
using MoonWorks.Graphics;

namespace Tactician;

internal class Program {
    public static string UserDataDirectory =
        $"{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Tactician")}";

    private static void Main(string[] args) {
        if (!Directory.Exists(UserDataDirectory)) Directory.CreateDirectory(UserDataDirectory);

        AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;

#if DEBUG
        var windowCreateInfo = new WindowCreateInfo {
            WindowWidth = 1280,
            WindowHeight = 720,
            WindowTitle = "Tactician",
            ScreenMode = ScreenMode.Windowed
        };
#else
			WindowCreateInfo windowCreateInfo = new WindowCreateInfo
			{
				WindowWidth = 1280,
				WindowHeight = 720,
				WindowTitle = "Tactician",
				ScreenMode = ScreenMode.Fullscreen
			};
#endif

        var framePacingSettings = FramePacingSettings.CreateLatencyOptimized(60);

        var debugMode = false;

#if DEBUG
        debugMode = true;
#endif

        var game = new TacticianGame(
            windowCreateInfo,
            framePacingSettings,
            ShaderFormat.SPIRV | ShaderFormat.DXBC | ShaderFormat.MSL,
            debugMode
        );

        game.Run();
    }

    private static void HandleUnhandledException(object sender, UnhandledExceptionEventArgs args) {
        var e = (Exception)args.ExceptionObject;
        Logger.LogError("Unhandled exception caught!");
        Logger.LogError(e.ToString());

        Game.ShowRuntimeError("FLAGRANT SYSTEM ERROR", e.ToString());

        var streamWriter = new StreamWriter(Path.Combine(UserDataDirectory, "log.txt"));

        streamWriter.WriteLine(e.ToString());
        streamWriter.Flush();
        streamWriter.Close();
    }
}