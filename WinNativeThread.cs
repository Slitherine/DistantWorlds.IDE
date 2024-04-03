using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DistantWorlds.IDE;

public readonly partial struct WinNativeThread : IDisposable {

    public readonly nint Handle;

    private WinNativeThread(nint handle)
        => Handle = handle;

    public WinNativeThread(nint handle, bool duplicate = false) {
        // must duplicate when given the pseudo=handle for current thread (-2)

        if (!duplicate && handle != -2) {
            Handle = handle;
            return;
        }

        if (!DuplicateHandle(-1, handle, -1,
                out var dup, 0, false,
                /*DUPLICATE_SAME_ACCESS*/2))
            throw new Win32Exception(Marshal.GetLastWin32Error());

        Handle = dup;
    }

    public void Resume() {
        if (ResumeThread(Handle) == -1)
            throw new Win32Exception(Marshal.GetLastWin32Error());
    }

    public void Suspend() {
        if (SuspendThread(Handle) == -1)
            throw new Win32Exception(Marshal.GetLastWin32Error());
    }

    public bool IsAlive {
        get {
            if (!GetExitCodeThread(Handle, out var code)
                || code != 259)
                return false;

            //CpuTimeUsed() != 0;

            GetTimes(out var created, out var exited, out var kernel, out var user);

            if (exited != 0)
                return false;

            if (user == 0)
                return false;

            return true;
        }
    }

    public static unsafe WinNativeThread Create<T>(delegate* unmanaged<T*, void> start, T* parameter,
        out uint threadId) where T : unmanaged
        => Create((delegate* unmanaged<void*, void>)start, parameter, out threadId);

    public static unsafe WinNativeThread Create<T>(delegate* unmanaged<T*, void> start, T* parameter = default)
        where T : unmanaged
        => Create(start, parameter, out _);

    public static unsafe WinNativeThread Create(delegate* unmanaged<void*, void> start, void* parameter,
        out uint threadId) {
        var handle = CreateThread(IntPtr.Zero, 0, start, parameter,
            /*CREATE_SUSPENDED*/4, out threadId);
        if (handle == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error());

        return new(handle);
    }

    public static unsafe WinNativeThread Create(delegate* unmanaged<void*, void> start, void* parameter)
        => Create(start, parameter, out _);

    public void Dispose()
        => CloseHandle(Handle);

    public void Deconstruct(out nint handle)
        => handle = Handle;

    public static implicit operator nint(WinNativeThread thread) => thread.Handle;

    public static implicit operator nint(WinNativeThread? thread) => thread?.Handle ?? default;

    public ulong CpuTimeUsed() {
        if (!QueryThreadCycleTime(Handle, out var cycleTime))
            throw new Win32Exception(Marshal.GetLastWin32Error());

        return cycleTime;
    }

    public void GetTimes(out long creationTime, out long exitTime, out long kernelTime, out long userTime) {
        exitTime = 0;
        if (!GetThreadTimes(Handle, out creationTime, ref exitTime, out kernelTime, out userTime))
            throw new Win32Exception(Marshal.GetLastWin32Error());
    }

    public uint ThreadId => GetThreadId(Handle);

    public string Description {
        get => GetThreadDescription(Handle, out var description) == default
            ? throw new Win32Exception(Marshal.GetLastWin32Error())
            : description;
        set {
            if (SetThreadDescription(Handle, value) == default)
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    public override string ToString() => $"Native Windows Thread #{ThreadId}: {Description}";

    public static WinNativeThread Current => new(-2, true);

}