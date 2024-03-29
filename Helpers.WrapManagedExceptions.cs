using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace DistantWorlds.IDE;

public static partial class Helpers {

    [Conditional("TRACE")]
    private static void TraceError(Exception ex)
        //=> Trace.TraceError(ex.ToString());
        => Console.Error.WriteLine(ex.ToString());

    // 0
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void UnwrapManagedExceptions(delegate *<void> f) {
        try {
            f();
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
        }
    }

    // 0-0
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void UnwrapManagedExceptions<T1>(delegate *<T1, void> f, T1 a1) {
        try {
            f(a1);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
        }
    }

    // 0-1
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void UnwrapManagedExceptions<T1>(delegate *<T1*, void> f, T1* a1)
        where T1 : unmanaged {
        try {
            f(a1);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
        }
    }

    // 0-00
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void UnwrapManagedExceptions<T1, T2>(delegate *<T1, T2, void> f, T1 a1, T2 a2) {
        try {
            f(a1, a2);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
        }
    }

    // 0-10
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void UnwrapManagedExceptions<T1, T2>(delegate *<T1*, T2, void> f, T1* a1, T2 a2)
        where T1 : unmanaged {
        try {
            f(a1, a2);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
        }
    }

    // 0-01
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void UnwrapManagedExceptions<T1, T2>(delegate *<T1, T2*, void> f, T1 a1, T2* a2)
        where T2 : unmanaged {
        try {
            f(a1, a2);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
        }
    }

    // 0-11
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void UnwrapManagedExceptions<T1, T2>(delegate *<T1*, T2*, void> f, T1* a1, T2* a2)
        where T1 : unmanaged where T2 : unmanaged {
        try {
            f(a1, a2);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
        }
    }

    // 0-000
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void UnwrapManagedExceptions<T1, T2, T3>(delegate *<T1, T2, T3, void> f, T1 a1, T2 a2, T3 a3) {
        try {
            f(a1, a2, a3);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
        }
    }

    // 0-100
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void UnwrapManagedExceptions<T1, T2, T3>(delegate *<T1*, T2, T3, void> f, T1* a1, T2 a2, T3 a3)
        where T1 : unmanaged {
        try {
            f(a1, a2, a3);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
        }
    }

    // 0-010
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void UnwrapManagedExceptions<T1, T2, T3>(delegate *<T1, T2*, T3, void> f, T1 a1, T2* a2, T3 a3)
        where T2 : unmanaged {
        try {
            f(a1, a2, a3);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
        }
    }

    // 0-101
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void UnwrapManagedExceptions<T1, T2, T3>(delegate *<T1*, T2, T3*, void> f, T1* a1, T2 a2,
        T3* a3)
        where T1 : unmanaged where T3 : unmanaged {
        try {
            f(a1, a2, a3);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
        }
    }

    // 0-011
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void UnwrapManagedExceptions<T1, T2, T3>(delegate *<T1, T2*, T3*, void> f, T1 a1, T2* a2,
        T3* a3)
        where T2 : unmanaged where T3 : unmanaged {
        try {
            f(a1, a2, a3);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
        }
    }

    // 0-110
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void UnwrapManagedExceptions<T1, T2, T3>(delegate *<T1*, T2*, T3, void> f, T1* a1, T2* a2,
        T3 a3)
        where T1 : unmanaged where T2 : unmanaged {
        try {
            f(a1, a2, a3);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
        }
    }

    // 0-111
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void UnwrapManagedExceptions<T1, T2, T3>(delegate *<T1*, T2*, T3*, void> f, T1* a1, T2* a2,
        T3* a3)
        where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged {
        try {
            f(a1, a2, a3);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
        }
    }

    // 0-0000
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void UnwrapManagedExceptions<T1, T2, T3, T4>(delegate *<T1, T2, T3, T4, void> f, T1 a1, T2 a2,
        T3 a3, T4 a4) {
        try {
            f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
        }
    }

    // 0-1000
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void UnwrapManagedExceptions<T1, T2, T3, T4>(delegate *<T1*, T2, T3, T4, void> f,
        T1* a1, T2 a2, T3 a3, T4 a4)
        where T1 : unmanaged {
        try {
            f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
        }
    }

    // 0-0100
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void UnwrapManagedExceptions<T1, T2, T3, T4>(delegate *<T1, T2*, T3, T4, void> f,
        T1 a1, T2* a2, T3 a3, T4 a4)
        where T2 : unmanaged {
        try {
            f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
        }
    }

    // 0-1100
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void UnwrapManagedExceptions<T1, T2, T3, T4>(delegate *<T1*, T2*, T3, T4, void> f,
        T1* a1, T2* a2, T3 a3, T4 a4)
        where T1 : unmanaged where T2 : unmanaged {
        try {
            f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
        }
    }

    // 0-0010
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void UnwrapManagedExceptions<T1, T2, T3, T4>(delegate *<T1, T2, T3*, T4, void> f, T1 a1, T2 a2,
        T3* a3, T4 a4)
        where T3 : unmanaged {
        try {
            f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
        }
    }

    // 0-1010
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void UnwrapManagedExceptions<T1, T2, T3, T4>(delegate *<T1*, T2, T3*, T4, void> f,
        T1* a1, T2 a2, T3* a3, T4 a4)
        where T1 : unmanaged where T3 : unmanaged {
        try {
            f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
        }
    }

    // 0-0110
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void UnwrapManagedExceptions<T1, T2, T3, T4>(delegate *<T1, T2*, T3*, T4, void> f,
        T1 a1, T2* a2, T3* a3, T4 a4)
        where T2 : unmanaged where T3 : unmanaged {
        try {
            f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
        }
    }

    // 0-1110
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void UnwrapManagedExceptions<T1, T2, T3, T4>(delegate *<T1*, T2*, T3*, T4, void> f,
        T1* a1, T2* a2, T3* a3, T4 a4)
        where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged {
        try {
            f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
        }
    }

    // 0-0001
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void UnwrapManagedExceptions<T1, T2, T3, T4>(delegate *<T1, T2, T3, T4*, void> f, T1 a1, T2 a2,
        T3 a3, T4* a4)
        where T4 : unmanaged {
        try {
            f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
        }
    }

    // 0-1001
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void UnwrapManagedExceptions<T1, T2, T3, T4>(delegate *<T1*, T2, T3, T4*, void> f,
        T1* a1, T2 a2, T3 a3, T4* a4)
        where T1 : unmanaged where T4 : unmanaged {
        try {
            f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
        }
    }

    // 0-0101
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void UnwrapManagedExceptions<T1, T2, T3, T4>(delegate *<T1, T2*, T3, T4*, void> f,
        T1 a1, T2* a2, T3 a3, T4* a4)
        where T2 : unmanaged where T4 : unmanaged {
        try {
            f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
        }
    }

    // 0-1101
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void UnwrapManagedExceptions<T1, T2, T3, T4>(delegate *<T1*, T2*, T3, T4*, void> f,
        T1* a1, T2* a2, T3 a3, T4* a4)
        where T1 : unmanaged where T2 : unmanaged where T4 : unmanaged {
        try {
            f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
        }
    }

    // 0-0011
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void UnwrapManagedExceptions<T1, T2, T3, T4>(delegate *<T1, T2, T3*, T4*, void> f,
        T1 a1, T2 a2, T3* a3, T4* a4)
        where T3 : unmanaged where T4 : unmanaged {
        try {
            f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
        }
    }

    // 0-1011
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void UnwrapManagedExceptions<T1, T2, T3, T4>(delegate *<T1*, T2, T3*, T4*, void> f,
        T1* a1, T2 a2, T3* a3, T4* a4)
        where T1 : unmanaged where T3 : unmanaged where T4 : unmanaged {
        try {
            f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
        }
    }

    // 0-0111
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void UnwrapManagedExceptions<T1, T2, T3, T4>(delegate *<T1, T2*, T3*, T4*, void> f,
        T1 a1, T2* a2, T3* a3, T4* a4)
        where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged {
        try {
            f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
        }
    }

    // 0-1111
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void UnwrapManagedExceptions<T1, T2, T3, T4>(delegate *<T1*, T2*, T3*, T4*, void> f,
        T1* a1, T2* a2, T3* a3, T4* a4)
        where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged {
        try {
            f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
        }
    }

    // 1
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* UnwrapManagedExceptions(delegate *<void*> f) {
        try {
            return f();
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 1-0
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* UnwrapManagedExceptions<T1>(delegate *<T1, void*> f, T1 a1) {
        try {
            return f(a1);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 1-1
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* UnwrapManagedExceptions<T1>(delegate *<T1*, void*> f, T1* a1)
        where T1 : unmanaged {
        try {
            return f(a1);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 1-00
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* UnwrapManagedExceptions<T1, T2>(delegate *<T1, T2, void*> f, T1 a1, T2 a2) {
        try {
            return f(a1, a2);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 1-10
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* UnwrapManagedExceptions<T1, T2>(delegate *<T1*, T2, void*> f, T1* a1, T2 a2)
        where T1 : unmanaged {
        try {
            return f(a1, a2);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 1-01
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* UnwrapManagedExceptions<T1, T2>(delegate *<T1, T2*, void*> f, T1 a1, T2* a2)
        where T2 : unmanaged {
        try {
            return f(a1, a2);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 1-11
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* UnwrapManagedExceptions<T1, T2>(delegate *<T1*, T2*, void*> f,
        T1* a1, T2* a2)
        where T1 : unmanaged where T2 : unmanaged {
        try {
            return f(a1, a2);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 1-000
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* UnwrapManagedExceptions<T1, T2, T3>(delegate *<T1, T2, T3, void*> f,
        T1 a1, T2 a2, T3 a3) {
        try {
            return f(a1, a2, a3);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 1-100
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* UnwrapManagedExceptions<T1, T2, T3>(delegate *<T1*, T2, T3, void*> f,
        T1* a1, T2 a2, T3 a3)
        where T1 : unmanaged {
        try {
            return f(a1, a2, a3);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 1-010
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* UnwrapManagedExceptions<T1, T2, T3>(delegate *<T1, T2*, T3, void*> f,
        T1 a1, T2* a2, T3 a3)
        where T2 : unmanaged {
        try {
            return f(a1, a2, a3);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 1-110
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* UnwrapManagedExceptions<T1, T2, T3>(delegate *<T1*, T2*, T3, void*> f,
        T1* a1, T2* a2, T3 a3)
        where T1 : unmanaged where T2 : unmanaged {
        try {
            return f(a1, a2, a3);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 1-001
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* UnwrapManagedExceptions<T1, T2, T3>(delegate *<T1, T2, T3*, void*> f,
        T1 a1, T2 a2, T3* a3)
        where T3 : unmanaged {
        try {
            return f(a1, a2, a3);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 1-101
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* UnwrapManagedExceptions<T1, T2, T3>(delegate *<T1*, T2, T3*, void*> f,
        T1* a1, T2 a2, T3* a3)
        where T1 : unmanaged where T3 : unmanaged {
        try {
            return f(a1, a2, a3);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 1-011
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* UnwrapManagedExceptions<T1, T2, T3>(delegate *<T1, T2*, T3*, void*> f,
        T1 a1, T2* a2, T3* a3)
        where T2 : unmanaged where T3 : unmanaged {
        try {
            return f(a1, a2, a3);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 1-111
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* UnwrapManagedExceptions<T1, T2, T3>(delegate *<T1*, T2*, T3*, void*> f,
        T1* a1, T2* a2, T3* a3)
        where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged {
        try {
            return f(a1, a2, a3);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 1-0000
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* UnwrapManagedExceptions<T1, T2, T3, T4>(delegate *<T1, T2, T3, T4, void*> f,
        T1 a1, T2 a2, T3 a3, T4 a4) {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 1-1000
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* UnwrapManagedExceptions<T1, T2, T3, T4>(
        delegate *<T1*, T2, T3, T4, void*> f,
        T1* a1, T2 a2, T3 a3, T4 a4)
        where T1 : unmanaged {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 1-0100
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* UnwrapManagedExceptions<T1, T2, T3, T4>(
        delegate *<T1, T2*, T3, T4, void*> f,
        T1 a1, T2* a2, T3 a3, T4 a4)
        where T2 : unmanaged {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 1-1100
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* UnwrapManagedExceptions<T1, T2, T3, T4>(
        delegate *<T1*, T2*, T3, T4, void*> f,
        T1* a1, T2* a2, T3 a3, T4 a4)
        where T1 : unmanaged where T2 : unmanaged {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 1-0010
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* UnwrapManagedExceptions<T1, T2, T3, T4>(
        delegate *<T1, T2, T3*, T4, void*> f,
        T1 a1, T2 a2, T3* a3, T4 a4)
        where T3 : unmanaged {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 1-1010
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* UnwrapManagedExceptions<T1, T2, T3, T4>(
        delegate *<T1*, T2, T3*, T4, void*> f,
        T1* a1, T2 a2, T3* a3, T4 a4)
        where T1 : unmanaged where T3 : unmanaged {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 1-0110
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* UnwrapManagedExceptions<T1, T2, T3, T4>(
        delegate *<T1, T2*, T3*, T4, void*> f,
        T1 a1, T2* a2, T3* a3, T4 a4)
        where T2 : unmanaged where T3 : unmanaged {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 1-1110
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* UnwrapManagedExceptions<T1, T2, T3, T4>(
        delegate *<T1*, T2*, T3*, T4, void*> f,
        T1* a1, T2* a2, T3* a3, T4 a4)
        where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 1-0001
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* UnwrapManagedExceptions<T1, T2, T3, T4>(
        delegate *<T1, T2, T3, T4*, void*> f,
        T1 a1, T2 a2, T3 a3, T4* a4)
        where T4 : unmanaged {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 1-1001
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* UnwrapManagedExceptions<T1, T2, T3, T4>(
        delegate *<T1*, T2, T3, T4*, void*> f,
        T1* a1, T2 a2, T3 a3, T4* a4)
        where T1 : unmanaged where T4 : unmanaged {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 1-0101
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* UnwrapManagedExceptions<T1, T2, T3, T4>(
        delegate *<T1, T2*, T3, T4*, void*> f,
        T1 a1, T2* a2, T3 a3, T4* a4)
        where T2 : unmanaged where T4 : unmanaged {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 1-1101
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* UnwrapManagedExceptions<T1, T2, T3, T4>(
        delegate *<T1*, T2*, T3, T4*, void*> f,
        T1* a1, T2* a2, T3 a3, T4* a4)
        where T1 : unmanaged where T2 : unmanaged where T4 : unmanaged {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 1-0011
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* UnwrapManagedExceptions<T1, T2, T3, T4>(
        delegate *<T1, T2, T3*, T4*, void*> f,
        T1 a1, T2 a2, T3* a3, T4* a4)
        where T3 : unmanaged where T4 : unmanaged {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 1-1011
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* UnwrapManagedExceptions<T1, T2, T3, T4>(
        delegate *<T1*, T2, T3*, T4*, void*> f,
        T1* a1, T2 a2, T3* a3, T4* a4)
        where T1 : unmanaged where T3 : unmanaged where T4 : unmanaged {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 1-0111
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* UnwrapManagedExceptions<T1, T2, T3, T4>(
        delegate *<T1, T2*, T3*, T4*, void*> f,
        T1 a1, T2* a2, T3* a3, T4* a4)
        where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 1-1111
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* UnwrapManagedExceptions<T1, T2, T3, T4>(
        delegate *<T1*, T2*, T3*, T4*, void*> f,
        T1* a1, T2* a2, T3* a3, T4* a4)
        where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 2
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult UnwrapManagedExceptions<TResult>(delegate *<TResult> f) {
        try {
            return f();
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 2-0
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult UnwrapManagedExceptions<T1, TResult>(delegate *<T1, TResult> f, T1 a1) {
        try {
            return f(a1);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 2-1
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult UnwrapManagedExceptions<T1, TResult>(delegate *<T1*, TResult> f, T1* a1)
        where T1 : unmanaged {
        try {
            return f(a1);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 2-00
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult UnwrapManagedExceptions<T1, T2, TResult>(delegate *<T1, T2, TResult> f, T1 a1, T2 a2) {
        try {
            return f(a1, a2);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 2-10
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult UnwrapManagedExceptions<T1, T2, TResult>(delegate *<T1*, T2, TResult> f, T1* a1, T2 a2)
        where T1 : unmanaged {
        try {
            return f(a1, a2);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 2-01
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult UnwrapManagedExceptions<T1, T2, TResult>(delegate *<T1, T2*, TResult> f, T1 a1, T2* a2)
        where T2 : unmanaged {
        try {
            return f(a1, a2);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 2-11
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult UnwrapManagedExceptions<T1, T2, TResult>(delegate *<T1*, T2*, TResult> f,
        T1* a1, T2* a2)
        where T1 : unmanaged where T2 : unmanaged {
        try {
            return f(a1, a2);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 2-000
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult UnwrapManagedExceptions<T1, T2, T3, TResult>(delegate *<T1, T2, T3, TResult> f,
        T1 a1, T2 a2, T3 a3) {
        try {
            return f(a1, a2, a3);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 2-100
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult UnwrapManagedExceptions<T1, T2, T3, TResult>(delegate *<T1*, T2, T3, TResult> f,
        T1* a1, T2 a2, T3 a3)
        where T1 : unmanaged {
        try {
            return f(a1, a2, a3);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 2-010
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult UnwrapManagedExceptions<T1, T2, T3, TResult>(delegate *<T1, T2*, T3, TResult> f,
        T1 a1, T2* a2, T3 a3)
        where T2 : unmanaged {
        try {
            return f(a1, a2, a3);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 2-110
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult UnwrapManagedExceptions<T1, T2, T3, TResult>(delegate *<T1*, T2*, T3, TResult> f,
        T1* a1, T2* a2, T3 a3)
        where T1 : unmanaged where T2 : unmanaged {
        try {
            return f(a1, a2, a3);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 2-001
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult UnwrapManagedExceptions<T1, T2, T3, TResult>(delegate *<T1, T2, T3*, TResult> f,
        T1 a1, T2 a2, T3* a3)
        where T3 : unmanaged {
        try {
            return f(a1, a2, a3);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 2-101
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult UnwrapManagedExceptions<T1, T2, T3, TResult>(delegate *<T1*, T2, T3*, TResult> f,
        T1* a1, T2 a2, T3* a3)
        where T1 : unmanaged where T3 : unmanaged {
        try {
            return f(a1, a2, a3);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 2-011
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult UnwrapManagedExceptions<T1, T2, T3, TResult>(delegate *<T1, T2*, T3*, TResult> f,
        T1 a1, T2* a2, T3* a3)
        where T2 : unmanaged where T3 : unmanaged {
        try {
            return f(a1, a2, a3);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 2-111
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult UnwrapManagedExceptions<T1, T2, T3, TResult>(delegate *<T1*, T2*, T3*, TResult> f,
        T1* a1, T2* a2, T3* a3)
        where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged {
        try {
            return f(a1, a2, a3);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 2-0000
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult UnwrapManagedExceptions<T1, T2, T3, T4, TResult>(delegate *<T1, T2, T3, T4, TResult> f,
        T1 a1, T2 a2, T3 a3, T4 a4) {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 2-1000
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult UnwrapManagedExceptions<T1, T2, T3, T4, TResult>(
        delegate *<T1*, T2, T3, T4, TResult> f,
        T1* a1, T2 a2, T3 a3, T4 a4)
        where T1 : unmanaged {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 2-0100
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult UnwrapManagedExceptions<T1, T2, T3, T4, TResult>(
        delegate *<T1, T2*, T3, T4, TResult> f,
        T1 a1, T2* a2, T3 a3, T4 a4)
        where T2 : unmanaged {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 2-1100
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult UnwrapManagedExceptions<T1, T2, T3, T4, TResult>(
        delegate *<T1*, T2*, T3, T4, TResult> f,
        T1* a1, T2* a2, T3 a3, T4 a4)
        where T1 : unmanaged where T2 : unmanaged {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 2-0010
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult UnwrapManagedExceptions<T1, T2, T3, T4, TResult>(
        delegate *<T1, T2, T3*, T4, TResult> f,
        T1 a1, T2 a2, T3* a3, T4 a4)
        where T3 : unmanaged {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 2-1010
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult UnwrapManagedExceptions<T1, T2, T3, T4, TResult>(
        delegate *<T1*, T2, T3*, T4, TResult> f,
        T1* a1, T2 a2, T3* a3, T4 a4)
        where T1 : unmanaged where T3 : unmanaged {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 2-0110
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult UnwrapManagedExceptions<T1, T2, T3, T4, TResult>(
        delegate *<T1, T2*, T3*, T4, TResult> f,
        T1 a1, T2* a2, T3* a3, T4 a4)
        where T2 : unmanaged where T3 : unmanaged {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 2-1110
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult UnwrapManagedExceptions<T1, T2, T3, T4, TResult>(
        delegate *<T1*, T2*, T3*, T4, TResult> f,
        T1* a1, T2* a2, T3* a3, T4 a4)
        where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 2-0001
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult UnwrapManagedExceptions<T1, T2, T3, T4, TResult>(
        delegate *<T1, T2, T3, T4*, TResult> f,
        T1 a1, T2 a2, T3 a3, T4* a4)
        where T4 : unmanaged {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 2-1001
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult UnwrapManagedExceptions<T1, T2, T3, T4, TResult>(
        delegate *<T1*, T2, T3, T4*, TResult> f,
        T1* a1, T2 a2, T3 a3, T4* a4)
        where T1 : unmanaged where T4 : unmanaged {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 2-0101
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult UnwrapManagedExceptions<T1, T2, T3, T4, TResult>(
        delegate *<T1, T2*, T3, T4*, TResult> f,
        T1 a1, T2* a2, T3 a3, T4* a4)
        where T2 : unmanaged where T4 : unmanaged {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 2-1101
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult UnwrapManagedExceptions<T1, T2, T3, T4, TResult>(
        delegate *<T1*, T2*, T3, T4*, TResult> f,
        T1* a1, T2* a2, T3 a3, T4* a4)
        where T1 : unmanaged where T2 : unmanaged where T4 : unmanaged {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 2-0011
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult UnwrapManagedExceptions<T1, T2, T3, T4, TResult>(
        delegate *<T1, T2, T3*, T4*, TResult> f,
        T1 a1, T2 a2, T3* a3, T4* a4)
        where T3 : unmanaged where T4 : unmanaged {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 2-1011
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult UnwrapManagedExceptions<T1, T2, T3, T4, TResult>(
        delegate *<T1*, T2, T3*, T4*, TResult> f,
        T1* a1, T2 a2, T3* a3, T4* a4)
        where T1 : unmanaged where T3 : unmanaged where T4 : unmanaged {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 2-0111
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult UnwrapManagedExceptions<T1, T2, T3, T4, TResult>(
        delegate *<T1, T2*, T3*, T4*, TResult> f,
        T1 a1, T2* a2, T3* a3, T4* a4)
        where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 2-1111
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult UnwrapManagedExceptions<T1, T2, T3, T4, TResult>(
        delegate *<T1*, T2*, T3*, T4*, TResult> f,
        T1* a1, T2* a2, T3* a3, T4* a4)
        where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 3
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult* UnwrapManagedExceptions<TResult>(delegate *<TResult*> f)
        where TResult : unmanaged {
        try {
            return f();
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 3-0
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult* UnwrapManagedExceptions<T1, TResult>(delegate *<T1, TResult*> f, T1 a1)
        where TResult : unmanaged {
        try {
            return f(a1);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 3-1
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult* UnwrapManagedExceptions<T1, TResult>(delegate *<T1*, TResult*> f, T1* a1)
        where TResult : unmanaged
        where T1 : unmanaged {
        try {
            return f(a1);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 3-00
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult* UnwrapManagedExceptions<T1, T2, TResult>(delegate *<T1, T2, TResult*> f, T1 a1, T2 a2)
        where TResult : unmanaged {
        try {
            return f(a1, a2);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 3-10
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult* UnwrapManagedExceptions<T1, T2, TResult>(delegate *<T1*, T2, TResult*> f, T1* a1,
        T2 a2)
        where TResult : unmanaged
        where T1 : unmanaged {
        try {
            return f(a1, a2);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 3-01
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult* UnwrapManagedExceptions<T1, T2, TResult>(delegate *<T1, T2*, TResult*> f, T1 a1,
        T2* a2)
        where TResult : unmanaged
        where T2 : unmanaged {
        try {
            return f(a1, a2);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 3-11
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult* UnwrapManagedExceptions<T1, T2, TResult>(delegate *<T1*, T2*, TResult*> f,
        T1* a1, T2* a2)
        where TResult : unmanaged
        where T1 : unmanaged
        where T2 : unmanaged {
        try {
            return f(a1, a2);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 3-000
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult* UnwrapManagedExceptions<T1, T2, T3, TResult>(delegate *<T1, T2, T3, TResult*> f,
        T1 a1, T2 a2, T3 a3)
        where TResult : unmanaged {
        try {
            return f(a1, a2, a3);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 3-100
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult* UnwrapManagedExceptions<T1, T2, T3, TResult>(delegate *<T1*, T2, T3, TResult*> f,
        T1* a1, T2 a2, T3 a3)
        where TResult : unmanaged
        where T1 : unmanaged {
        try {
            return f(a1, a2, a3);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 3-010
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult* UnwrapManagedExceptions<T1, T2, T3, TResult>(delegate *<T1, T2*, T3, TResult*> f,
        T1 a1, T2* a2, T3 a3)
        where TResult : unmanaged
        where T2 : unmanaged {
        try {
            return f(a1, a2, a3);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 3-110
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult* UnwrapManagedExceptions<T1, T2, T3, TResult>(delegate *<T1*, T2*, T3, TResult*> f,
        T1* a1, T2* a2, T3 a3)
        where TResult : unmanaged
        where T1 : unmanaged
        where T2 : unmanaged {
        try {
            return f(a1, a2, a3);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 3-001
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult* UnwrapManagedExceptions<T1, T2, T3, TResult>(delegate *<T1, T2, T3*, TResult*> f,
        T1 a1, T2 a2, T3* a3)
        where TResult : unmanaged
        where T3 : unmanaged {
        try {
            return f(a1, a2, a3);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 3-101
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult* UnwrapManagedExceptions<T1, T2, T3, TResult>(delegate *<T1*, T2, T3*, TResult*> f,
        T1* a1, T2 a2, T3* a3)
        where TResult : unmanaged
        where T1 : unmanaged
        where T3 : unmanaged {
        try {
            return f(a1, a2, a3);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 3-011
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult* UnwrapManagedExceptions<T1, T2, T3, TResult>(delegate *<T1, T2*, T3*, TResult*> f,
        T1 a1, T2* a2, T3* a3)
        where TResult : unmanaged
        where T2 : unmanaged
        where T3 : unmanaged {
        try {
            return f(a1, a2, a3);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 3-111
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult* UnwrapManagedExceptions<T1, T2, T3, TResult>(delegate *<T1*, T2*, T3*, TResult*> f,
        T1* a1, T2* a2, T3* a3)
        where TResult : unmanaged
        where T1 : unmanaged
        where T2 : unmanaged
        where T3 : unmanaged {
        try {
            return f(a1, a2, a3);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 3-0000
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult* UnwrapManagedExceptions<T1, T2, T3, T4, TResult>(
        delegate *<T1, T2, T3, T4, TResult*> f,
        T1 a1, T2 a2, T3 a3, T4 a4)
        where TResult : unmanaged {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 3-1000
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult* UnwrapManagedExceptions<T1, T2, T3, T4, TResult>(
        delegate *<T1*, T2, T3, T4, TResult*> f,
        T1* a1, T2 a2, T3 a3, T4 a4)
        where TResult : unmanaged
        where T1 : unmanaged {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 3-0100
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult* UnwrapManagedExceptions<T1, T2, T3, T4, TResult>(
        delegate *<T1, T2*, T3, T4, TResult*> f,
        T1 a1, T2* a2, T3 a3, T4 a4)
        where TResult : unmanaged
        where T2 : unmanaged {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 3-1100
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult* UnwrapManagedExceptions<T1, T2, T3, T4, TResult>(
        delegate *<T1*, T2*, T3, T4, TResult*> f,
        T1* a1, T2* a2, T3 a3, T4 a4)
        where TResult : unmanaged
        where T1 : unmanaged
        where T2 : unmanaged {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 3-0010
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult* UnwrapManagedExceptions<T1, T2, T3, T4, TResult>(
        delegate *<T1, T2, T3*, T4, TResult*> f,
        T1 a1, T2 a2, T3* a3, T4 a4)
        where TResult : unmanaged
        where T3 : unmanaged {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 3-1010
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult* UnwrapManagedExceptions<T1, T2, T3, T4, TResult>(
        delegate *<T1*, T2, T3*, T4, TResult*> f,
        T1* a1, T2 a2, T3* a3, T4 a4)
        where TResult : unmanaged
        where T1 : unmanaged
        where T3 : unmanaged {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 3-0110
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult* UnwrapManagedExceptions<T1, T2, T3, T4, TResult>(
        delegate *<T1, T2*, T3*, T4, TResult*> f,
        T1 a1, T2* a2, T3* a3, T4 a4)
        where TResult : unmanaged
        where T2 : unmanaged
        where T3 : unmanaged {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 3-1110
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult* UnwrapManagedExceptions<T1, T2, T3, T4, TResult>(
        delegate *<T1*, T2*, T3*, T4, TResult*> f,
        T1* a1, T2* a2, T3* a3, T4 a4)
        where TResult : unmanaged
        where T1 : unmanaged
        where T2 : unmanaged
        where T3 : unmanaged {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 3-0001
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult* UnwrapManagedExceptions<T1, T2, T3, T4, TResult>(
        delegate *<T1, T2, T3, T4*, TResult*> f,
        T1 a1, T2 a2, T3 a3, T4* a4)
        where TResult : unmanaged
        where T4 : unmanaged {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 3-1001
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult* UnwrapManagedExceptions<T1, T2, T3, T4, TResult>(
        delegate *<T1*, T2, T3, T4*, TResult*> f,
        T1* a1, T2 a2, T3 a3, T4* a4)
        where TResult : unmanaged
        where T1 : unmanaged
        where T4 : unmanaged {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 3-0101
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult* UnwrapManagedExceptions<T1, T2, T3, T4, TResult>(
        delegate *<T1, T2*, T3, T4*, TResult*> f,
        T1 a1, T2* a2, T3 a3, T4* a4)
        where TResult : unmanaged
        where T2 : unmanaged
        where T4 : unmanaged {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 3-1101
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult* UnwrapManagedExceptions<T1, T2, T3, T4, TResult>(
        delegate *<T1*, T2*, T3, T4*, TResult*> f,
        T1* a1, T2* a2, T3 a3, T4* a4)
        where TResult : unmanaged
        where T1 : unmanaged
        where T2 : unmanaged
        where T4 : unmanaged {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 3-0011
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult* UnwrapManagedExceptions<T1, T2, T3, T4, TResult>(
        delegate *<T1, T2, T3*, T4*, TResult*> f,
        T1 a1, T2 a2, T3* a3, T4* a4)
        where TResult : unmanaged
        where T3 : unmanaged
        where T4 : unmanaged {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 3-1011
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult* UnwrapManagedExceptions<T1, T2, T3, T4, TResult>(
        delegate *<T1*, T2, T3*, T4*, TResult*> f,
        T1* a1, T2 a2, T3* a3, T4* a4)
        where TResult : unmanaged
        where T1 : unmanaged
        where T3 : unmanaged
        where T4 : unmanaged {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 3-0111
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult* UnwrapManagedExceptions<T1, T2, T3, T4, TResult>(
        delegate *<T1, T2*, T3*, T4*, TResult*> f,
        T1 a1, T2* a2, T3* a3, T4* a4)
        where TResult : unmanaged
        where T2 : unmanaged
        where T3 : unmanaged
        where T4 : unmanaged {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

    // 3-1111
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TResult* UnwrapManagedExceptions<T1, T2, T3, T4, TResult>(
        delegate *<T1*, T2*, T3*, T4*, TResult*> f,
        T1* a1, T2* a2, T3* a3, T4* a4)
        where TResult : unmanaged
        where T1 : unmanaged
        where T2 : unmanaged
        where T3 : unmanaged
        where T4 : unmanaged {
        try {
            return f(a1, a2, a3, a4);
        }
        catch (Exception e) {
            TraceError(e);
            Exports.LastException = ExceptionDispatchInfo.Capture(e);
            return default!;
        }
    }

}