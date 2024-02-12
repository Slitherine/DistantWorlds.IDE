using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using GameFinder.RegistryUtils;
using GameFinder.Common;
using GameFinder.StoreHandlers.GOG;
using GameFinder.StoreHandlers.Steam;
using GameFinder.StoreHandlers.Steam.Models.ValueTypes;
using Microsoft.Win32;
using NexusMods.Paths;
using RegistryHive = GameFinder.RegistryUtils.RegistryHive;
using RegistryValueKind = Microsoft.Win32.RegistryValueKind;
using RegistryView = GameFinder.RegistryUtils.RegistryView;

namespace DistantWorlds.IDE;

[SuppressMessage("Usage", "VSTHRD001:Avoid legacy thread switching APIs")]
public static partial class Dw2Env {

    public const int GogGameId = 1562671603;

    public const int SteamAppId = 1531540;

    public const string UserChosenGameDirectoryRegKeyPath = @"SOFTWARE\Matrix Games\Distant Worlds 2\IDE";

    public const string UserChosenGameDirectoryRegValuePath = $@"{UserChosenGameDirectoryRegKeyPath}\GameDirectory";

    private static string? _gameDirectory;

    public static string? GameDirectory {
        get => _gameDirectory;
        set {
            if (string.IsNullOrWhiteSpace(value)) {
                _gameDirectory = null;
                return;
            }

            // normalize
            _gameDirectory = new Uri(value).LocalPath
                .TrimEnd('\\');
        }
    }

    public static IGame? Game;

    private static OpenFileDialog _openFileDialog;

    public static readonly string DefaultMatrixInstallerGameDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
        "Matrix Games", "Distant Worlds 2");

    [SupportedOSPlatform("windows")]
    public static string? UserChosenGameDirectory {
        get => (string?)Registry.CurrentUser.GetValue(UserChosenGameDirectoryRegValuePath, null,
            RegistryValueOptions.DoNotExpandEnvironmentNames);
        set {
            if (value is null)
                Registry.CurrentUser.DeleteValue(UserChosenGameDirectoryRegValuePath);
            else {
                Registry.CurrentUser.CreateSubKey(UserChosenGameDirectoryRegKeyPath);
                Registry.CurrentUser.SetValue(UserChosenGameDirectoryRegValuePath, value, RegistryValueKind.String);
            }
        }
    }

    public static string? MatrixInstallerRegistryGameDirectory {
        get {
            if (!OperatingSystem.IsWindows())
                return null;

            string? dw2PathFromReg = null;
            try {
                using var w32HkLocalMac =
                    WindowsRegistry.Shared.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
                using var dw2Key = w32HkLocalMac.OpenSubKey(@"SOFTWARE\Matrix Games\Distant Worlds 2");
                if (dw2Key is not null)
                    if (!dw2Key.TryGetString("installed to", out dw2PathFromReg))
                        dw2PathFromReg = null;
            }
            catch {
                // oof
            }

            return dw2PathFromReg;
        }
    }

    public static bool GameDirectoryIsValid
        => GameDirectory is not null
            && Directory.Exists(GameDirectory
            )
            && File.Exists(Path.Combine(GameDirectory, "DistantWorlds2.exe"));

    public static bool CheckGameDirectory(bool cancelIsExit = false) {
        while (!GameDirectoryIsValid) {
            if (!PromptForGameDirectory(cancelIsExit))
                return false;
        }

        return true;
    }

    public static bool PromptForGameDirectory(bool cancelIsExit = false) {
        var choice = MessageBox.Show(
            $"""
             Distant Worlds 2 is required to use this IDE.
             Current Game Directory: {GameDirectory ?? "None selected."}
             Press OK to select a new game directory.
             Press Cancel to {(cancelIsExit ? "exit the application" : "continue without changing it")}.
             """,
            "Game Directory",
            MessageBoxButtons.OKCancel,
            MessageBoxType.Question);

        if (choice == DialogResult.Cancel) {
            if (cancelIsExit)
                Environment.Exit(1);
            return false;
        }

        var ofdDir = GameDirectory ?? Environment.CurrentDirectory;
        var ofd = new OpenFileDialog {
            Filters = { "Distant Worlds 2 Executable|DistantWorlds2.exe", },
            Title = "Select Distant Worlds 2 Executable",
            Directory = new(ofdDir),
            CheckFileExists = true,
            FileName = Path.Combine(ofdDir, "DistantWorlds2.exe")
        };
        var result = ofd.ShowDialog(null);
        if (result != DialogResult.Cancel) {
            GameDirectory = Path.GetDirectoryName(ofd.FileName);
            if (OperatingSystem.IsWindows())
                UserChosenGameDirectory = GameDirectory;
        }

        return true;
    }

    static Dw2Env() {
        EtoPlatform = new Eto.GtkSharp.Platform();
        Application = new Application(EtoPlatform);
        _openFileDialog = new OpenFileDialog();

        if (OperatingSystem.IsWindows() && UserChosenGameDirectory is not null) {
            GameDirectory = UserChosenGameDirectory;
            CheckGameDirectory();
            return;
        }

        var fs = FileSystem.Shared;
        var registry = OperatingSystem.IsWindows() ? WindowsRegistry.Shared : null;
        var steam = new SteamHandler(fs, registry);
        // ReSharper disable once NotAccessedVariable // TODO: log any errors
        var steamDw2 = steam.FindOneGameById(AppId.From(SteamAppId), out var errors);
        if (steamDw2 is not null) {
            Game = steamDw2;
            GameDirectory = steamDw2.Path.GetFullPath();
        }
        else if (registry is not null) {
            var gog = new GOGHandler(registry, fs);
            var gogDw2 = gog.FindOneGameById(GOGGameId.From(GogGameId), out errors);
            if (gogDw2 is not null) {
                Game = gogDw2;
                GameDirectory = gogDw2.Path.GetFullPath();
                if (CheckGameDirectory()) {
                    PromptForGameDirectory();
                    return;
                }
            }
            else {
                var mgDw2 = MatrixInstallerRegistryGameDirectory;
                if (mgDw2 is not null) {
                    GameDirectory = DefaultMatrixInstallerGameDirectory;
                    if (CheckGameDirectory()) {
                        PromptForGameDirectory();
                        return;
                    }
                }
                else {
                    GameDirectory = DefaultMatrixInstallerGameDirectory;
                    if (CheckGameDirectory()) {
                        PromptForGameDirectory();
                        return;
                    }
                }
            }
        }

        PromptForGameDirectory(true);
    }

    public static readonly Application Application;

    internal static Eto.GtkSharp.Platform EtoPlatform;

    /// <see cref="M:DistantWorlds.IDE.Dw2Env.#cctor"/>
    public static void Initialize() {
    }

}