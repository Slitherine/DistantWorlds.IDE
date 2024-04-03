using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DNNE;

namespace DistantWorlds.IDE;

[SkipLocalsInit]
public static class GuiContext {

    [STAThread]
#if DW2IDE_GUI_THREAD_NATIVE
    [UnmanagedCallersOnly]
    public static unsafe void GuiThreadWorker( [C99Type("void*")]Empty* _) {
        var thread = Thread.CurrentThread;
        thread.Name = "DW2IDE GUI Thread";
        thread.SetApartmentState(ApartmentState.STA);
#else
    public static void GuiThreadWorker(object? _) {
#endif
        SynchronizationContext.SetSynchronizationContext(Dw2Env.GuiThreadContext);
        if (!Dw2Env.IsDefaultContext)
            throw new InvalidOperationException("GUI thread must be started from the default context");

        for (;;) {
            try {
                if (Dw2Env.GuiThreadContext.WaitForWork(125))
                    Dw2Env.GuiThreadContext.ExecuteQueue();
            }
            catch (OperationCanceledException oce) when (oce.CancellationToken == Dw2Env.GuiThreadContext.CancellationToken) {
                break;
            }
            catch (Exception e) {
                Trace.TraceError("Unhandled exception on GUI thread:\n{0}", e);
            }
        }

        Trace.TraceInformation("GUI thread exiting");
    }

}