using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DNNE;

namespace DistantWorlds.IDE;

[SkipLocalsInit]
public static class GuiContext {
    
    public static readonly SimpleSynchronizationContext GuiThreadContext
        = new();
    
#if DW2IDE_GUI_THREAD_NATIVE
    public static readonly unsafe WinNativeThread GuiThread
        = WinNativeThread.Create<Empty>(&GuiContext.GuiThreadWorker);
#else
    public static readonly unsafe Thread GuiThread
        = new Thread(GuiThreadWorker) {
            IsBackground = true,
            Name = "DW2IDE GUI Thread"
        };
#endif
    
    [STAThread]
#if DW2IDE_GUI_THREAD_NATIVE
    [UnmanagedCallersOnly]
    public static unsafe void GuiThreadWorker([C99Type("void*")]Empty* _) {
        var thread = Thread.CurrentThread;
        thread.Name = "DW2IDE GUI Thread";
        thread.SetApartmentState(ApartmentState.STA);
#else
    public static void GuiThreadWorker(object? _) {
#endif
        SynchronizationContext.SetSynchronizationContext(GuiThreadContext);
        if (!IsolationEnvironment.IsDefault)
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

        Trace.TraceInformation("GUI thread exiting");
    }

}