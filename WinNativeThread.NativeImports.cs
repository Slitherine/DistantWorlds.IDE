using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DistantWorlds.IDE;

public partial struct WinNativeThread {

    private const string Lib = "kernel32";

    [LibraryImport(Lib, SetLastError = true)]
    private static unsafe partial nint CreateThread(nint lpThreadAttributes, uint dwStackSize,
        void* lpStartAddress, void* lpParameter,
        uint dwCreationFlags, out uint lpThreadId);

    [LibraryImport(Lib, SetLastError = true)]
    private static partial int ResumeThread(nint hThread);

    [LibraryImport(Lib, SetLastError = true)]
    private static partial int SuspendThread(nint hThread);

    [LibraryImport(Lib, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.I4)]
    private static partial bool GetExitCodeThread(nint hThread, out uint lpExitCode);

    [LibraryImport(Lib, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.I4)]
    private static partial bool CloseHandle(nint hObject);

    [LibraryImport(Lib, SetLastError = true)]
    private static partial uint GetThreadId(nint thread);

    [LibraryImport(Lib, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.I4)]
    private static partial bool QueryThreadCycleTime(nint threadHandle, out ulong cycleTime);
    
    [LibraryImport(Lib, SetLastError = true)]
    private static partial nint SetThreadDescription(nint hThread,
        [MarshalAs(UnmanagedType.LPWStr)] string lpThreadDescription);
    
    [LibraryImport(Lib, SetLastError = true)]
    private static partial nint GetThreadDescription(nint hThread,
        [MarshalAs(UnmanagedType.LPWStr)] out string lpThreadDescription);

    [LibraryImport(Lib, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.I4)]
    private static partial bool GetThreadTimes(nint hThread,
        out long lpCreationTime, ref long lpExitTime, out long lpKernelTime, out long lpUserTime);

    [LibraryImport(Lib, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.I4)]
    private static partial bool DuplicateHandle(nint hSourceProcessHandle, nint hSourceHandle,
        nint hTargetProcessHandle, out nint lpTargetHandle, uint dwDesiredAccess,
        [MarshalAs(UnmanagedType.I4)] bool bInheritHandle, uint dwOptions);

}