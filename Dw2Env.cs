using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;
using System.Runtime.Versioning;
using GameFinder.RegistryUtils;
using GameFinder.Common;
using GameFinder.StoreHandlers.GOG;
using GameFinder.StoreHandlers.Steam;
using GameFinder.StoreHandlers.Steam.Models.ValueTypes;
using MonoMod.Utils;
using NexusMods.Paths;
using RegistryHive = GameFinder.RegistryUtils.RegistryHive;
using Registry = Microsoft.Win32.Registry;
using RegistryValueOptions = Microsoft.Win32.RegistryValueOptions;
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

    private static OpenFileDialog OpenFileDialog;

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
    private static readonly AssemblyLoadContext CurrentContext
        = AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly())!;


    public static readonly bool IsDefaultContext
        = AssemblyLoadContext.Default
        == CurrentContext;

    public static readonly SimpleSynchronizationContext GuiThreadContext
        = new();

    public static readonly Thread GuiThread = IsDefaultContext ? new(GuiThreadWorker) {
        Name = "GUI Thread",
        IsBackground = true
    } : null!;

    private static void GuiThreadWorker() {
        SynchronizationContext.SetSynchronizationContext(GuiThreadContext);
        if (!IsDefaultContext)
            throw new InvalidOperationException("GUI thread must be started from the default context");
        
        for (;;) {
            try {
                if (GuiThreadContext.WaitForWork(125))
                    GuiThreadContext.ExecuteQueue();
            }
            catch (OperationCanceledException oce) when (oce.CancellationToken == GuiThreadContext.CancellationToken) {
                break;
            }
            catch (Exception e) {
                Trace.TraceError("Unhandled exception on GUI thread:\n{0}", e);
            }
        }
    }

    private static void StartGuiThread() {
        if (!IsDefaultContext || GuiThread.IsAlive)
            return;

        GuiThread.SetApartmentState(ApartmentState.STA);
        CurrentContext.Unloading += _ => GuiThreadContext.Cancel();
        AppDomain.CurrentDomain.ProcessExit += (_, _) => GuiThreadContext.Cancel();
        GuiThread.Start();
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
        OpenFileDialog ofd = null!;
        // ReSharper disable once HeapView.CanAvoidClosure
        var result = GuiThreadContext.Send(() => {
            ofd = new OpenFileDialog {
                Filters = { "Distant Worlds 2 Executable|DistantWorlds2.exe", },
                Title = "Select Distant Worlds 2 Executable",
                Directory = new(ofdDir),
                CheckFileExists = true,
                FileName = Path.Combine(ofdDir, "DistantWorlds2.exe")
            };
            return ofd.ShowDialog(null);
        });
        if (result != DialogResult.Cancel) {
            GameDirectory = Path.GetDirectoryName(ofd.FileName);
            if (OperatingSystem.IsWindows())
                UserChosenGameDirectory = GameDirectory;
        }

        return true;
    }

    private static unsafe T GetOrCreate<T>(Expression<Func<T>> proxyExpr, [AllowNull] ref T field,
        delegate * <T> createFn) where T : class {
        if (field is not null)
            return field;

        var asm = Assembly.GetExecutingAssembly();
        if (IsDefaultContext)
            return createFn();

        if (proxyExpr is null)
            throw new InvalidOperationException("Proxy expression must be provided");

        var defAsm = AssemblyLoadContext.Default.LoadFromAssemblyName(asm.GetName());
        if (proxyExpr.Body is not MethodCallExpression { Arguments.Count: 0 } callExpr)
            throw new InvalidOperationException("Proxy expression must be a method call with no arguments");

        var proxyFnToken = callExpr.Method.MetadataToken;
        var defMethod = defAsm.ManifestModule.ResolveMethod(proxyFnToken)!;
        if (!defMethod.IsStatic)
            throw new InvalidOperationException("Proxy method must be static");

        var fnPtr = (delegate *<T>)defMethod.MethodHandle.GetFunctionPointer();

        return field = fnPtr();
    }

    private static readonly Type EtoPlatformType
#if DW2IDE_WPF
        = typeof(Eto.Wpf.Platform);
#elif DW2IDE_WINFORMS
        = typeof(Eto.WinForms.Platform);
#elif DW2IDE_D2D
        = typeof(Eto.Direct2D.Platform);
#elif DW2IDE_GTK
        = typeof(Eto.GtkSharp.Platform);
#else
#error Specify an Eto GUI toolkit
#endif

    private static unsafe Eto.Platform GetOrCreateEtoPlatform() {
        return GetOrCreate(() => GetOrCreateEtoPlatform(), ref EtoPlatform, &Factory);

        static Eto.Platform Factory() => (Eto.Platform)Activator.CreateInstance(EtoPlatformType)!;
    }

    private static unsafe Application GetOrCreateApplication() {
        return GetOrCreate(() => GetOrCreateApplication(), ref Application, &Factory);

        static Application Factory() => new(EtoPlatform);
    }

    private static unsafe OpenFileDialog GetOrCreateOpenFileDialog() {
        return GetOrCreate(() => GetOrCreateOpenFileDialog(), ref OpenFileDialog, &Factory);

        static OpenFileDialog Factory() => new();
    }

    static Dw2Env() {
        StartGuiThread();

        EtoPlatform = GetOrCreateEtoPlatform();
        Application = GetOrCreateApplication();
        OpenFileDialog = GetOrCreateOpenFileDialog();

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

    public static Application Application;

    internal static Eto.Platform EtoPlatform;

    /// <see cref="M:DistantWorlds.IDE.Dw2Env.#cctor"/>
    public static void Initialize() {
    }

}