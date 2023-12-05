using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using GameFinder.RegistryUtils;
using GameFinder.Common;
using GameFinder.RegistryUtils;
using GameFinder.StoreHandlers.GOG;
using GameFinder.StoreHandlers.Steam;
using GameFinder.StoreHandlers.Steam.Models.ValueTypes;
using Microsoft.Win32;
using NexusMods.Paths;
using OmniSharp.Extensions.JsonRpc;
using RegistryHive = GameFinder.RegistryUtils.RegistryHive;
using RegistryValueKind = Microsoft.Win32.RegistryValueKind;
using RegistryView = GameFinder.RegistryUtils.RegistryView;

namespace DW2IDE;

[SuppressMessage("Usage", "VSTHRD001:Avoid legacy thread switching APIs")]
public class Dw2Env {

  public static void Init() {
    /* kick off static init */
  }

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

  private OpenFileDialog _openFileDialog = new OpenFileDialog();

  public static readonly string DefaultMatrixInstallerGameDirectory = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
    "Matrix Games", "Distant Worlds 2");

  public static string? UserChosenGameDirectory {
    get => (string?)Registry.CurrentUser.GetValue(UserChosenGameDirectoryRegValuePath, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
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
      string? dw2PathFromReg = null;
      try {
        using var w32HkLocalMac = WindowsRegistry.Shared.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
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
      MessageBoxButton.OKCancel,
      MessageBoxImage.Question);

    if (choice == MessageBoxResult.Cancel) {
      if (cancelIsExit)
        Environment.Exit(1);
      return false;
    }

    var ofdDir = GameDirectory ?? Environment.CurrentDirectory;
    var ofd = new OpenFileDialog {
      Filter = "Distant Worlds 2 Executable|DistantWorlds2.exe",
      Title = "Select Distant Worlds 2 Executable",
      InitialDirectory = ofdDir,
      CheckFileExists = true,
      FileName = Path.Combine(ofdDir, "DistantWorlds2.exe"),
      ValidateNames = true
    };
    var result = ofd.ShowDialog();
    if (result == true) {
      GameDirectory = Path.GetDirectoryName(ofd.FileName);
      UserChosenGameDirectory = GameDirectory;
    }

    Application.Current.Dispatcher.Invoke(() => {
      Application.Current.MainWindow?.BringToFront();
    });

    return true;
  }

  static Dw2Env() {
    if (UserChosenGameDirectory is not null) {
      GameDirectory = UserChosenGameDirectory;
      CheckGameDirectory();
      return;
    }

    var fs = FileSystem.Shared;
    var registry = WindowsRegistry.Shared;
    var steam = new SteamHandler(fs, registry);
    var steamDw2 = steam.FindOneGameById(AppId.From(SteamAppId), out var errors);
    if (steamDw2 is not null) {
      Game = steamDw2;
      GameDirectory = steamDw2.Path.GetFullPath();
    }
    else {
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

}