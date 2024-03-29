using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using DistantWorlds.IDE.Stride;
using Minimatch;
using MonoMod.Utils;
using Stride.Core.Storage;
using SixLabors.ImageSharp;
using Stride.Graphics;
using static DistantWorlds.IDE.Dw2Env;
using Image = Stride.Graphics.Image;
using CBool = byte;

// ReSharper disable UseSymbolAlias

namespace DistantWorlds.IDE;

/// <remarks>
/// Some notes on DNNE:
/// DNNE MSBuild output messages only show up at diagnostic level normal and above.
/// It will silently not update the generated source file if there is an error.
/// bool is not understood by DNNE, use byte values 0 and 1.
/// </remarks>
public static class Exports {

    private static readonly Type Type = typeof(Exports);

    private static readonly Assembly Assembly = Type.Assembly;

    private static readonly AssemblyLoadContext LoadContext =
        AssemblyLoadContext.GetLoadContext(Assembly)!;

    private static readonly bool IsInDefaultContext = LoadContext == AssemblyLoadContext.Default;

    private static int _isolationContextCounter;

    [ThreadStatic]
    internal static ExceptionDispatchInfo? LastException;

    public static unsafe int GetNewIsolationContextId(object o) {
        if (IsInDefaultContext) {
            var ctx = GetIsolationContext(o);
            if (ctx == null)
                throw new NotImplementedException("Could not get isolation context.");
            var h = new GCHandle<AssemblyLoadContext>(ctx, GCHandleType.Weak);
            int id;
            do id = Interlocked.Increment(ref _isolationContextCounter);
            while (!ImmutableInterlocked.TryAdd(ref _isolatedContexts, id, h));
            if (id != 0)
                return id;

            DebugBreakImpl();
            throw new("Somehow created the default context.");
        }

        // enter default context to rely on single instance of _isolationContextCounter

        var asm = AssemblyLoadContext.Default.Assemblies.FirstOrDefault
            (asm => asm.GetName().Name == Assembly.GetName().Name);
        if (asm is null) {
            // ah come on MS
            //AssemblyLoadContext.Default.Resolving += ResolveAssembliesCrossLoadContext;
            asm = AssemblyLoadContext.Default.LoadFromAssemblyName(Assembly.GetName());
            //AssemblyLoadContext.Default.Resolving -= ResolveAssembliesCrossLoadContext;
        }

        var type = asm.GetType(Type.FullName!);
        var mi = type?.GetMethod(nameof(GetNewIsolationContextId));
        //var value = prop?.Invoke(null, null);
        if (mi == null) return -1;

        /*if (mi.GetCustomAttribute<UnmanagedCallersOnlyAttribute>() != null)
            throw new NotImplementedException("GetNewIsolationContextId has unmanaged callers only.");*/

        var fn = (delegate*<object, int>)mi.GetLdftnPointer();
        var newCtxId = fn(o);

        if (newCtxId != 0)
            return newCtxId;

        DebugBreakImpl();
        throw new("Somehow created the default context.");
    }

    private static unsafe delegate *<int, string, void*> _defCtxGetExport = null;

    public static unsafe void* GetExport(int isoCtxId, string name) {
        if (isoCtxId < 0) {
            DebugBreakImpl();
            return null;
        }

        if (IsInDefaultContext) {
            if (isoCtxId != 0)
                return (void*)ImmutableInterlocked.GetOrAdd(ref _isolatedExports, (isoCtxId, name),
                    IsolatedFuncPointerFactory);

            var mi = Type.GetMethod($"{name}Impl");
            if (mi is null) return null;

            /*if (mi.GetCustomAttribute<UnmanagedCallersOnlyAttribute>() != null)
                throw new NotImplementedException($"{name}Impl has unmanaged callers only.");*/

            return (void*)mi.GetLdftnPointer();
        }

        if (_defCtxGetExport != null)
            return _defCtxGetExport(isoCtxId, name);

        var asm = AssemblyLoadContext.Default.Assemblies.FirstOrDefault
            (asm => asm.GetName().Name == Assembly.GetName().Name);
        var type = asm?.GetType(Type.FullName!);
        var exportMi = type?.GetMethod(nameof(GetExport));
        if (exportMi == null) return null;

        /*if (exportMi.GetCustomAttribute<UnmanagedCallersOnlyAttribute>() != null)
            throw new NotImplementedException($"GetExport has unmanaged callers only.");*/

        _defCtxGetExport = (delegate*<int, string, void*>)exportMi.GetLdftnPointer();

        return _defCtxGetExport == null ? null : _defCtxGetExport(isoCtxId, name);
    }

    private static nint IsolatedFuncPointerFactory((int isoCtxId, string name) k) {
        if (!_isolatedContexts.TryGetValue(k.isoCtxId, out var exports)) {
            DebugBreakImpl();
            return 0;
        }

        if (!exports.IsAllocated)
            return 0;

        var alc = exports.Target;
        
        if (alc is null) {
            DebugBreakImpl();
            return 0;
        }

        var pFn = GetFunctionPointerFromLoadContext(alc, Type.FullName!, $"{k.name}Impl");

        return pFn;
    }

    // @formatter:off
    private class Dummy {
        public static readonly Dummy Instance = new();
    }
    // @formatter:on

    public static readonly int IsolationContextId
        = IsInDefaultContext ? 0 : GetNewIsolationContextId(Dummy.Instance);

    public static readonly AssemblyLoadContext IsolationContext
        = IsInDefaultContext
            ? AssemblyLoadContext.Default
            : AssemblyLoadContext.GetLoadContext(Assembly)!;

    private static ConditionalWeakTable<AssemblyLoadContext, StrongBox<int>> _contextIds =
        IsInDefaultContext ? new() : null!;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int _GetIsolationContextId(AssemblyLoadContext? ctx)
        => ctx is null
            ? -1
            : IsInDefaultContext
                ? _contextIds.TryGetValue(ctx, out var id)
                    ? id.Value
                    : -1
                : ResolveIsolationContextId(ctx);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ResolveIsolationContextId(AssemblyLoadContext ctx)
        => IsolatedInvoke(ctx, () => GetIsolationContextIdImpl());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int _GetIsolationContextId(Assembly? asm)
        => asm is null ? -1 : _GetIsolationContextId(AssemblyLoadContext.GetLoadContext(asm)!);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int _GetIsolationContextId(Type? t)
        => t is null ? -1 : _GetIsolationContextId(t.Assembly);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // ReSharper disable once UnusedMember.Local
    private static int _GetIsolationContextId(object? obj)
        => obj is null ? -1 : _GetIsolationContextId(obj.GetType());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static AssemblyLoadContext? GetIsolationContext(Assembly? asm)
        => asm is null ? null : AssemblyLoadContext.GetLoadContext(asm)!;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static AssemblyLoadContext? GetIsolationContext(Type? t)
        => t is null ? null : GetIsolationContext(t.Assembly);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static AssemblyLoadContext? GetIsolationContext(object? obj)
        => obj is null ? null : GetIsolationContext(obj.GetType());

    private static object? Resolve(Expression? expr) {
        if (expr == null) return null;

        switch (expr.NodeType) {
            case ExpressionType.Constant:
                return ((ConstantExpression)expr).Value;
            case ExpressionType.MemberAccess:
                var ma = (MemberExpression)expr;
                var root = Resolve(ma.Expression);
                var member = ma.Member;
                return member switch {
                    FieldInfo fi => fi.GetValue(root),
                    PropertyInfo pi => pi.GetValue(root),
                    _ => throw new NotImplementedException($"Not implemented member access: {member.MemberType}")
                };
            default:
                throw new NotImplementedException($"Not implemented node type: {expr.NodeType}");
        }
    }

    private static Type ResolveType(AssemblyLoadContext isoCtx, Type type) {
        var asmName = type.Assembly.GetName();
        var typeName = type.FullName;
        var isoAsm = isoCtx.Assemblies.FirstOrDefault(asm => asm.GetName().Name == asmName.Name)
            ?? isoCtx.LoadFromAssemblyName(asmName);
        var isoType = isoAsm.GetType(typeName!);
        if (isoType is null) throw new NotImplementedException();

        return isoType;
    }

    private static Type[] ResolveTypes(AssemblyLoadContext isoCtx, IEnumerable<Type> types) {
        return types.Select(type => ResolveType(isoCtx, type)).ToArray();
    }

    private static MethodBase GetIsolated(AssemblyLoadContext isoCtx, MethodBase mb) {
        var type = mb.ReflectedType!;
        var asm = type.Assembly;
        var asmName = asm.GetName();
        var typeName = type.FullName;
        var methodName = mb.Name;
        var paramTypes = ResolveTypes(isoCtx, mb.GetParameters().Select(p => p.ParameterType));

        var isoAsm = isoCtx.Assemblies
                .FirstOrDefault(a => a.GetName().Name == asmName.Name)
            ?? isoCtx.LoadFromAssemblyName(asmName);

        var isoType = isoAsm.GetType(typeName!);
        if (isoType is null) throw new NotImplementedException();

        var bf = (mb.IsPublic ? BindingFlags.Public : BindingFlags.NonPublic)
            | (mb.IsStatic ? BindingFlags.Static : BindingFlags.Instance);

        var isoMethod = isoType.GetMethod(methodName, bf, paramTypes);
        if (isoMethod is null)
            throw new NotImplementedException("Didn't find method");

        return isoMethod;
    }

    private static TResult? IsolatedInvoke<TResult>(AssemblyLoadContext ctx, Expression<Func<TResult>> expr)
        => (TResult?)IsolatedInvokeImpl(ctx, expr);

    private static object? IsolatedInvokeImpl(AssemblyLoadContext ctx, Expression expr) {
        for (;;) {
            switch (expr.NodeType) {
                case ExpressionType.Lambda:
                    var lambda = (LambdaExpression)expr;
                    expr = lambda.Body;
                    continue;

                case ExpressionType.Constant:
                    var constExpr = (ConstantExpression)expr;
                    return constExpr.Value;

                case ExpressionType.Call:
                    var call = (MethodCallExpression)expr;
                    var method = call.Method;
                    var target = call.Object;
                    var args = call.Arguments.Select(Resolve).ToArray();
                    var isoMethod = GetIsolated(ctx, method);
                    if (isoMethod is null) throw new NotImplementedException();
                    //var invoker = isoMethod.GetFastInvoker();
                    //return invoker.Invoke(method.IsStatic ? null : Resolve(target!), args);

                    /*if (isoMethod.GetCustomAttribute<UnmanagedCallersOnlyAttribute>() != null)
                        throw new NotImplementedException("Not getting an isolated version of an unmanaged method.");*/

                    return isoMethod.Invoke(method.IsStatic ? null : Resolve(target!), args);

                default:
                    throw new NotImplementedException($"Not implemented node type: {expr.NodeType}");
            }
        }
    }

    private static ImmutableDictionary<int, GCHandle<AssemblyLoadContext>> _isolatedContexts =
        IsInDefaultContext ? ImmutableDictionary<int, GCHandle<AssemblyLoadContext>>.Empty : null!;

    private static ImmutableDictionary<(int IsolationContextId, string ExportName), nint> _isolatedExports
        = IsInDefaultContext ? ImmutableDictionary<(int, string), nint>.Empty : null!;

    // C++ / C99 boolean support
    private const byte True = 1;

    private const byte False = 0;

    static Exports() {
        // some quick assertions
        Debug.Assert(ObjectId.HashSize == 16);
        Debug.Assert(ObjectId.HashStringLength == 32);

#if TRACE
        Trace.Listeners.Add(new ConsoleTraceListener(true));
#endif
    }

    [DNNE.C99DeclCode( //language=c++
        $"""
         #ifndef __cplusplus
         #include <stdbool.h>
         #endif

         typedef void(* StreamWrite_t)(void* state, unsigned char* buffer, size_t length);

         const size_t ObjectIdByteSize = 16;
         const size_t ObjectIdStringLength = 32;

         """
        + Interop.Array.C
        + Interop.Utf8String.C
        + Interop.Utf16String.C
        + Interop.Request.C
        + Interop.Response.C
        + //language=c++
        """
        typedef enum MessageBoxButtons {
          OK,
          OKCancel,
          YesNo,
          YesNoCancel,
        } MessageBoxButtons_t;
        typedef enum MessageBoxType
        {
          Information,
          Warning,
          Error,
          Question,
        } MessageBoxType_t;
        typedef enum DialogResult
        {
          None,
          Ok,
          Cancel,
          Yes,
          No,
          Abort,
          Ignore,
          Retry,
        } DialogResult_t;

        """
    )]
    [UnmanagedCallersOnly, Obsolete("Do not call from managed code.", true)]
    public static unsafe void Initialize()
        => UnwrapManagedExceptions(&InitializeImpl);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void InitializeImpl() {
        Dw2Env.Initialize();
    }

    [UnmanagedCallersOnly, Obsolete("Do not call from managed code.", true)]
    public static unsafe int StartIsolationContext()
        => UnwrapManagedExceptions(&StartIsolationContextImpl);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe int StartIsolationContextImpl() {
        if (IsolationContext != AssemblyLoadContext.Default) {
            Trace.TraceInformation(
                "Dispatching to default context for creating new isolation context...");

            return IsolatedInvoke(AssemblyLoadContext.Default,
                () => StartIsolationContextImpl());
        }

        //var ctx = new AssemblyLoadContext(Guid.NewGuid().ToString("N"), true);
        var ctx = (AssemblyLoadContext)
            Activator.CreateInstance(IsoLoadCtxType,
                Assembly.Location)!;
        //ctx.Resolving += ResolveAssembliesCrossLoadContext;
        var pFn = GetFunctionPointerFromLoadContext(ctx,
            Type.FullName!, nameof(GetIsolationContextIdImpl));

        /*if (mi.GetCustomAttribute<UnmanagedCallersOnlyAttribute>() != null)
                throw new NotImplementedException($"GetIsolationContextIdImpl has unmanaged callers only.");*/
        var isoCtxId = ((delegate*<int>)pFn)();

        Trace.TraceInformation($"Created isolation context #{isoCtxId} ({ctx.Name})");

        if (isoCtxId != 0)
            return isoCtxId;

        DebugBreakImpl();
        throw new("Somehow created the default context.");
    }

    [UnmanagedCallersOnly, Obsolete("Do not call from managed code.", true)]
    public static unsafe CBool UnloadIsolationContext(int isoCtxId)
        => UnwrapManagedExceptions(&UnloadIsolationContextImpl, isoCtxId);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe CBool UnloadIsolationContextImpl(int isoCtxId) {
        if (isoCtxId == 0)
            return False;

        if (!IsInDefaultContext) {
            var pFn = (delegate *<int, bool>)GetExport(0, nameof(UnloadIsolationContext));
            pFn(isoCtxId);
        }

        GCHandle<AssemblyLoadContext> h;

        lock (_isolatedContexts) {
            if (!_isolatedContexts.Remove(isoCtxId, out h))
                return False;
        }

        ImmutableInterlocked.Update(ref _isolatedExports, isolatedExports => {
            return isolatedExports.RemoveRange
            (isolatedExports
                .Keys
                .Where(key => key.IsolationContextId == isoCtxId));
        });

        var ctx = h.Target;

        h.Free();

        if (ctx is null)
            // must already be unloaded
            return True;

        // try to not hold any references
        ThreadPool.QueueUserWorkItem
            (static ctx => ctx.Unload(), ctx, false);

        return True;
    }

    [UnmanagedCallersOnly, Obsolete("Do not call from managed code.", true)]
    public static unsafe int GetIsolationContextId()
        => UnwrapManagedExceptions(&GetIsolationContextIdImpl);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetIsolationContextIdImpl() => IsolationContextId;

    [UnmanagedCallersOnly, Obsolete("Do not call from managed code.", true)]
    public static void DebugBreak()
        => DebugBreakImpl();

    [MethodImpl(MethodImplOptions.AggressiveInlining), StackTraceHidden]
    private static void DebugBreakImpl() {
        Debugger.Launch();
        while (!Debugger.IsAttached)
            Debugger.Break();
    }

    [UnmanagedCallersOnly, Obsolete("Do not call from managed code.", true)]
    public static unsafe int GetVersion(char* pBuffer, int bufferSize)
        => UnwrapManagedExceptions(&GetVersionImpl, pBuffer, bufferSize);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe int GetVersionImpl(char* pBuffer, int bufferSize) {
        return ExtractUnmanagedString( // ide assembly version
            typeof(Dw2Env).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
            pBuffer, bufferSize);
    }

    [UnmanagedCallersOnly, Obsolete("Do not call from managed code.", true)]
    public static unsafe int GetNetVersion(char* pBuffer, int bufferSize)
        => UnwrapManagedExceptions(&GetNetVersionImpl, pBuffer, bufferSize);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe int GetNetVersionImpl(char* pBuffer, int bufferSize)
        => ExtractUnmanagedString( // net runtime version
            typeof(object).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
            pBuffer, bufferSize);

    /// <summary>
    /// Gets the game directory.
    /// </summary>
    /// <param name="pBuffer">Pointer to a buffer to write the game directory to.</param>
    /// <param name="bufferSize">Size of the buffer.</param>
    /// <returns>
    /// Zero when not set.
    /// On insufficient buffer size, the negative of the number of bytes required.
    /// The number of bytes written to the buffer.
    /// </returns>
    [UnmanagedCallersOnly, Obsolete("Do not call from managed code.", true)]
    public static unsafe int GetGameDirectory(char* pBuffer, int bufferSize)
        => UnwrapManagedExceptions(&GetGameDirectoryImpl, pBuffer, bufferSize);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe int GetGameDirectoryImpl(char* pBuffer, int bufferSize)
        => ExtractUnmanagedString(GameDirectory, pBuffer, bufferSize);

    /// <summary>
    /// Gets the user-chosen game directory.
    /// </summary>
    /// <param name="pBuffer">Pointer to a buffer to write the game directory to.</param>
    /// <param name="bufferSize">Size of the buffer.</param>
    /// <returns>
    /// Zero when not set.
    /// On insufficient buffer size, the negative of the number of bytes required.
    /// The number of bytes written to the buffer.
    /// </returns>
    [UnmanagedCallersOnly, Obsolete("Do not call from managed code.", true)]
    public static unsafe int GetUserChosenGameDirectory(char* pBuffer, int bufferSize)
        => UnwrapManagedExceptions(&GetUserChosenGameDirectoryImpl, pBuffer, bufferSize);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe int GetUserChosenGameDirectoryImpl(char* pBuffer, int bufferSize)
        => OperatingSystem.IsWindows()
            ? ExtractUnmanagedString(UserChosenGameDirectory, pBuffer, bufferSize)
            : 0;

    [UnmanagedCallersOnly, Obsolete("Do not call from managed code.", true)]
    public static unsafe void Deisolate()
        => UnwrapManagedExceptions(&DeisolateImpl);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DeisolateImpl()
        => Deisolator.Initialize();

    [UnmanagedCallersOnly, Obsolete("Do not call from managed code.", true)]
    public static unsafe void ReleaseHandle(nint handle)
        => UnwrapManagedExceptions(&ReleaseHandleImpl, handle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReleaseHandleImpl(nint handle) {
        var h = new GCHandle<object>(handle);
        /*var target = h.Target;
        if (target is IDisposable disposable)
            disposable.Dispose();*/
        // assume finializers will call Dispose
        h.Free();
    }

    [ThreadStatic]
    private static (WeakReference Ref, string? Str) _handleToStringCache;

    [UnmanagedCallersOnly, Obsolete("Do not call from managed code.", true)]
    public static unsafe int HandleToString(nint handle, char* buffer, int bufferLength)
        => UnwrapManagedExceptions(&HandleToStringImpl, handle, buffer, bufferLength);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe int HandleToStringImpl(nint handle, char* buffer, int bufferLength) {
        if (handle == 0) return 0;

        var obj = new GCHandle<object>(handle).Target;

        if (obj is null) return 0;

        if (_handleToStringCache is { Str: not null, Ref.IsAlive: true } &&
            _handleToStringCache.Ref.Target == obj)
            return ExtractUnmanagedString(_handleToStringCache.Str, buffer, bufferLength);

        string? str = null;

        if (obj is ExceptionDispatchInfo edi)
            try {
                /* From ExceptionDispatchInfo.Throw documentation;
                 * Throws the exception that's represented by the current
                 * ExceptionDispatchInfo object, after restoring the state
                 * that was saved when the exception was captured. */
                edi.Throw();
            }
            catch (Exception ex) {
                str = ex.ToString();
            }
        else
            str = obj.ToString();

        var result = ExtractUnmanagedString(str, buffer, bufferLength);
        if (result >= 0)
            return result;

        _handleToStringCache = (new(obj), str);
        return result;
    }

    [UnmanagedCallersOnly, Obsolete("Do not call from managed code.", true)]
    public static unsafe nint LoadBundle(char* path, int pathLength, int isoCtxId)
        => UnwrapManagedExceptions(&LoadBundleImpl, path, pathLength, isoCtxId);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe nint LoadBundleImpl(char* path, int pathLength, int isoCtxId) {
        if (isoCtxId != IsolationContextId)
            return ((delegate*<char*, int, int, nint>)GetExport(isoCtxId, nameof(LoadBundle)))
                (path, pathLength, isoCtxId);

        var chars = new ReadOnlySpan<char>(path, pathLength);
        var bundlePath = chars.IsEmpty ? null : new string(chars);
        //Console.WriteLine($"path: 0x{(nuint)path:X8} {bundlePath}");
        if (bundlePath is null) return default;

        try {
            var bundleName = BundleManager.LoadBundle(bundlePath);
            var loadedBundle = BundleManager.GetLoadedBundles()
                .FirstOrDefault(b => b.BundleName == bundleName);
            return loadedBundle is null
                ? default
                : new GCHandle<BundleManager.Bundle>(loadedBundle).Value;
        }
        catch {
            return default;
        }
    }

    [UnmanagedCallersOnly, Obsolete("Do not call from managed code.", true)]
    public static unsafe nint QueryBundleObjects(nint bundleHandle, char* glob, int globLength)
        => UnwrapManagedExceptions(&QueryBundleObjectsImpl, bundleHandle, glob, globLength);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe nint QueryBundleObjectsImpl(nint bundleHandle, char* glob, int globLength) {
        var h = new GCHandle<BundleManager.Bundle>(bundleHandle);
        var o = h.NonGeneric.Target;
        if (o is null) return default;

        if (o is not BundleManager.Bundle bundle) {
            var isoCtx = GetIsolationContext(o);
            if (isoCtx == IsolationContext || isoCtx == null)
                return default;

            Trace.TraceInformation(
                $"Dispatching to isolation context #{_GetIsolationContextId(isoCtx)} ({isoCtx.Name})");

            // redirect to the appropriate isolation context 
            return IsolatedInvoke(
                isoCtx,
                () => QueryBundleObjectsImpl(bundleHandle, glob, globLength));
        }

        var globSpan = new ReadOnlySpan<char>(glob, globLength);
        var globStr = globSpan.IsEmpty ? null : new string(globSpan);
        //Console.WriteLine($"glob: 0x{(nuint)glob:X8} {globStr}");
        var description = bundle.Description;
        var results = description.Assets.Select(a => a.Key);
        if (globStr != null && globStr != "**")
            results = new Minimatcher(globStr).Filter(results);

        var enumerator = results.GetEnumerator();
        if (!enumerator.MoveNext()) {
            enumerator.Dispose();
            return default;
        }

        return new GCHandle<IEnumerator<string>>(enumerator).Value;
    }

    [UnmanagedCallersOnly, Obsolete("Do not call from managed code.", true)]
    public static unsafe nint ReadQueriedBundleObject(nint bundleQueryHandle, char* buffer, int bufferLength)
        => UnwrapManagedExceptions(&ReadQueriedBundleObjectImpl, bundleQueryHandle, buffer, bufferLength);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe nint ReadQueriedBundleObjectImpl(nint bundleQueryHandle, char* buffer, int bufferLength) {
        var h = new GCHandle<IEnumerator<string>>(bundleQueryHandle);
        var o = h.NonGeneric.Target;
        if (o is null) return 0;

        if (o is not IEnumerator<string> enumerator) {
            var isoCtx = GetIsolationContext(o);
            if (isoCtx == IsolationContext || isoCtx == null)
                return 0;

            Trace.TraceInformation(
                $"Dispatching to isolation context #{_GetIsolationContextId(isoCtx)} ({isoCtx.Name})");

            // redirect to the appropriate isolation context 
            return IsolatedInvoke(
                isoCtx,
                () => ReadQueriedBundleObjectImpl(bundleQueryHandle, buffer, bufferLength));
        }

        try {
            try {
                var str = enumerator.Current;

                var result = ExtractUnmanagedString(str, buffer, bufferLength);
                if (result <= 0)
                    return result;

                if (enumerator.MoveNext())
                    return result;

                // enumeration completed
                enumerator.Dispose();
                h.Free();

                return result;
            }
            catch {
                h.Free();
            }
        }
        catch {
            // discard
        }

        return 0;
    }

    [UnmanagedCallersOnly, Obsolete("Do not call from managed code.", true)]
    [return: DNNE.C99Type("bool")]
    public static unsafe CBool TryGetObjectId(char* path, int pathLength, byte* pObjectId, int isoCtxId)
        => UnwrapManagedExceptions(&TryGetObjectIdImpl, path, pathLength, pObjectId, isoCtxId);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe CBool TryGetObjectIdImpl(char* path, int pathLength, byte* pObjectId, int isoCtxId) {
        if (pObjectId is null) return False;

        if (path is null || pathLength == 0) return False;

        if (isoCtxId != IsolationContextId)
            return ((delegate*<char*, int, byte*, int, CBool>)GetExport(isoCtxId, nameof(TryGetObjectId)))
                (path, pathLength, pObjectId, isoCtxId);

        var chars = new ReadOnlySpan<char>(path, pathLength);
        var contentPath = chars.IsEmpty ? null : new string(chars);
        var id = BundleManager.GetObjectId(contentPath!);
        if (id is null) return False;

        *(ObjectId*)pObjectId = id.Value;
        return True;
    }

    private static readonly Dictionary<string, nint> BundlePathStringMap = new();

    [UnmanagedCallersOnly, Obsolete("Do not call from managed code.", true)]
    [return: DNNE.C99Type("bool")]
    public static unsafe CBool TryGetObjectOffset(byte* pObjectId, long* pOffset, long* pOffsetEnd,
        char** pSourceDirOrBundlePath, int isoCtxId) {
        try {
            return TryGetObjectOffsetImpl(pObjectId, pOffset, pOffsetEnd, pSourceDirOrBundlePath, isoCtxId);
        }
        catch (Exception e) {
            Console.Error.WriteLine(e);
            return default!;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe CBool TryGetObjectOffsetImpl(byte* pObjectId, long* pOffset, long* pOffsetEnd,
        char** pSourceDirOrBundlePath, int isoCtxId) {
        if (pObjectId is null) return False;

        if (isoCtxId != IsolationContextId)
            return ((delegate*<byte*, long*, long*, char**, int, CBool>)GetExport(isoCtxId, nameof(TryGetObjectOffset)))
                (pObjectId, pOffset, pOffsetEnd, pSourceDirOrBundlePath, isoCtxId);

        var id = *(ObjectId*)pObjectId;

        var offset = BundleManager.GetObjectOffset(id, out var offsetEnd, out var sourceDirOrBundlePath);

        if (pOffset is not null)
            *pOffset = offset;

        if (pOffsetEnd is not null)
            *pOffsetEnd = offsetEnd;

        if (pSourceDirOrBundlePath is null)
            return True;

        if (sourceDirOrBundlePath is null) {
            *pSourceDirOrBundlePath = null;
            return True;
        }

        var path = string.Intern(sourceDirOrBundlePath);
        // callback is from single threaded js, but just in case...
        lock (BundlePathStringMap) {
            if (BundlePathStringMap.TryGetValue(path, out var pointer)) {
                *pSourceDirOrBundlePath = (char*)pointer;
                return True;
            }

            var chars = (char*)NativeMemory.Alloc((nuint)path.Length + 1, sizeof(char));
            path.AsSpan().CopyTo(new(chars, path.Length));
            chars[path.Length] = '\0';
            BundlePathStringMap.Add(path, (nint)chars);
            *pSourceDirOrBundlePath = chars;
            return True;
        }
    }

    [UnmanagedCallersOnly, Obsolete("Do not call from managed code.", true)]
    [return: DNNE.C99Type("bool")]
    public static unsafe CBool TryGetObjectSize(byte* pObjectId, long* pSize, int isoCtxId)
        => UnwrapManagedExceptions(&TryGetObjectSizeImpl,pObjectId, pSize, isoCtxId);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe CBool TryGetObjectSizeImpl(byte* pObjectId, long* pSize, int isoCtxId) {
        if (pObjectId is null) return False;

        if (isoCtxId != IsolationContextId)
            return ((delegate*<byte*, long*, int, CBool>)GetExport(isoCtxId, nameof(TryGetObjectSize)))
                (pObjectId, pSize, isoCtxId);

        var id = *(ObjectId*)pObjectId;

        var size = BundleManager.GetObjectSize(id);

        if (pSize is not null)
            *pSize = size;

        return True;
    }

    [ThreadStatic]
    private static (ObjectId Id, string? Value) _objectTypeCache;

    [UnmanagedCallersOnly, Obsolete("Do not call from managed code.", true)]
    public static unsafe int GetObjectType(byte* pObjectId,
        char* pObjectType, int objectTypeLength, int isoCtxId)
        => UnwrapManagedExceptions(&GetObjectTypeImpl,pObjectId, pObjectType, objectTypeLength, isoCtxId);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe int GetObjectTypeImpl(byte* pObjectId,
        char* pObjectType, int objectTypeLength, int isoCtxId) {
        if (pObjectId == null) return 0;

        if (isoCtxId != IsolationContextId)
            return ((delegate*<byte*, char*, int, int, int>)GetExport(isoCtxId, nameof(GetObjectType)))
                (pObjectId, pObjectType, objectTypeLength, isoCtxId);

        var id = *(ObjectId*)pObjectId;

        if (_objectTypeCache.Id == id)
            return ExtractUnmanagedString(_objectTypeCache.Value, pObjectType, objectTypeLength);

        var type = BundleManager.GetObjectQualifiedType(id);
        _objectTypeCache = (id, type);
        return ExtractUnmanagedString(type, pObjectType, objectTypeLength);
    }

    [ThreadStatic]
    private static (ObjectId Id, string? Value) _objectSimplifiedTypeCache;

    // public static string? GetObjectSimplifiedType(ObjectId? id)
    [UnmanagedCallersOnly, Obsolete("Do not call from managed code.", true)]
    public static unsafe int GetObjectSimplifiedType(byte* pObjectId,
        char* pSimpleType, int simpleTypeLength, int isoCtxId)
        => UnwrapManagedExceptions(&GetObjectSimplifiedTypeImpl,pObjectId, pSimpleType, simpleTypeLength, isoCtxId);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe int GetObjectSimplifiedTypeImpl(byte* pObjectId,
        char* pSimpleType, int simpleTypeLength, int isoCtxId) {
        if (pObjectId == null) return 0;

        if (isoCtxId != IsolationContextId)
            return ((delegate*<byte*, char*, int, int, int>)GetExport(isoCtxId, nameof(GetObjectSimplifiedType)))
                (pObjectId, pSimpleType, simpleTypeLength, isoCtxId);

        var id = *(ObjectId*)pObjectId;

        if (_objectSimplifiedTypeCache.Id == id)
            return ExtractUnmanagedString(_objectSimplifiedTypeCache.Value, pSimpleType, simpleTypeLength);

        var simpleType = BundleManager.GetObjectSimplifiedType(id);
        _objectSimplifiedTypeCache = (id, simpleType);
        return ExtractUnmanagedString(simpleType, pSimpleType, simpleTypeLength);
    }

    [UnmanagedCallersOnly, Obsolete("Do not call from managed code.", true)]
    public static unsafe nint InstantiateBundleItem(char* url, int urlLength, int isoCtxId)
        => UnwrapManagedExceptions(&InstantiateBundleItemImpl,url, urlLength, isoCtxId);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe nint InstantiateBundleItemImpl(char* url, int urlLength, int isoCtxId) {
        if (isoCtxId != IsolationContextId)
            return ((delegate*<char*, int, int, nint>)GetExport(isoCtxId, nameof(InstantiateBundleItem)))
                (url, urlLength, isoCtxId);

        var chars = new ReadOnlySpan<char>(url, urlLength);
        var urlStr = chars.IsEmpty ? null : new string(chars);
        if (urlStr is null) return 0;

        try {
            var obj = BundleManager.InstantiateObject(urlStr);
            if (obj is null) return 0;

            var h = new GCHandle<object>(obj);
            return h.Value;
        }
        catch (Exception ex) {
            Console.Error.WriteLine($"Failed to instantiate bundle item: {urlStr}");
            if (Debugger.IsAttached)
                Debug.WriteLine(ex);
            return 0;
        }
    }


    [UnmanagedCallersOnly, Obsolete("Do not call from managed code.", true)]
    public static unsafe nint InstantiateBundleItemByObjectId(byte* pObjectId, int isoCtxId)
        => UnwrapManagedExceptions(&InstantiateBundleItemByObjectIdImpl,pObjectId, isoCtxId);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe nint InstantiateBundleItemByObjectIdImpl(byte* pObjectId, int isoCtxId) {
        if (isoCtxId != IsolationContextId)
            return ((delegate*<byte*, int, nint>)GetExport(isoCtxId, nameof(InstantiateBundleItemByObjectId)))
                (pObjectId, isoCtxId);
        
        var objId = (ObjectId*)pObjectId;
        
        if (objId == null) return 0;

        try {
            var obj = BundleManager.InstantiateObject(in *objId);
            if (obj is null) return 0;

            var h = new GCHandle<object>(obj);
            return h.Value;
        }
        catch (Exception ex) {
            Console.Error.WriteLine($"Failed to instantiate bundle item: {objId->ToString()}");
            if (Debugger.IsAttached)
                Debug.WriteLine(ex);
            return 0;
        }
    }

    [UnmanagedCallersOnly, Obsolete("Do not call from managed code.", true)]
    [return: DNNE.C99Type("bool")]
    public static unsafe CBool IsImage(nint handle)
        => UnwrapManagedExceptions(&IsImageImpl,handle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CBool IsImageImpl(nint handle) {
        var h = new GCHandle<Image>(handle);
        var o = h.NonGeneric.Target;
        if (o is Image) return True;

        var isoCtx = GetIsolationContext(o);
        if (isoCtx == IsolationContext || isoCtx == null)
            return False;

        Trace.TraceInformation(
            $"Dispatching to isolation context #{_GetIsolationContextId(isoCtx)} ({isoCtx.Name})");

        // redirect to the appropriate isolation context
        return IsolatedInvoke(isoCtx,
            () => IsImageImpl(handle));
    }

    [UnmanagedCallersOnly, Obsolete("Do not call from managed code.", true)]
    [return: DNNE.C99Type("bool")]
    public static unsafe CBool TryConvertImageToBufferWebp(nint handle, int mipLevel, byte* pBuffer, int bufferSize)
        => TryConvertImageToBufferWebpImpl(handle, mipLevel, pBuffer, bufferSize);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe CBool
        TryConvertImageToBufferWebpImpl(nint handle, int mipLevel, byte* pBuffer, int bufferSize) {
        var h = new GCHandle<Image>(handle);

        if (!h.TryGetTarget(out var img)) {
            var o = h.NonGeneric.Target;
            if (o is null) return False;

            var isoCtx = GetIsolationContext(o);
            if (isoCtx == IsolationContext || isoCtx == null)
                return False;

            Trace.TraceInformation(
                $"Dispatching to isolation context #{_GetIsolationContextId(isoCtx)} ({isoCtx.Name})");

            // redirect to the appropriate isolation context
            return IsolatedInvoke(isoCtx,
                () => TryConvertImageToBufferWebpImpl(handle, mipLevel, pBuffer, bufferSize));
        }

        try {
            var ums = new UnmanagedMemoryStream(pBuffer, bufferSize);
            BundleManager.ConvertToImages(img, false, (newImg, _) => {
                ums.Position = 0;
                newImg.SaveAsWebp(ums);
                ums.Position = 0;
            }, mipLevel);
        }
        catch {
            return False;
        }

        return True;
    }

    [UnmanagedCallersOnly, Obsolete("Do not call from managed code.", true)]
    [return: DNNE.C99Type("bool")]
    public static unsafe CBool TryConvertImageToStreamWebp(nint handle, int mipLevel,
        [DNNE.C99Type("StreamWrite_t")] void* pWriteFn, void* state)
        => TryConvertImageToStreamWebpImpl(handle, mipLevel, pWriteFn, state);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe CBool TryConvertImageToStreamWebpImpl(nint handle, int mipLevel,
        [DNNE.C99Type("StreamWrite_t")] void* pWriteFn, void* state) {
        var h = new GCHandle<Image>(handle);

        if (!h.TryGetTarget(out var img)) {
            var o = h.NonGeneric.Target;
            if (o is null) return False;

            var isoCtx = GetIsolationContext(o);
            if (isoCtx == IsolationContext || isoCtx == null)
                return False;

            Trace.TraceInformation(
                $"Dispatching to isolation context #{_GetIsolationContextId(isoCtx)} ({isoCtx.Name})");

            // redirect to the appropriate isolation context
            return IsolatedInvoke(isoCtx,
                () => TryConvertImageToStreamWebpImpl(handle, mipLevel, pWriteFn, state));
        }

        try {
            var writeFn = (delegate* unmanaged[Cdecl]<void*, void*, nuint, void>)pWriteFn;
            var ds = new UnmanagedDelegatedWriteOnlyStream(writeFn, state);
            using var ms = GetRecyclableMemoryStream();
            // can't just use ds here, the encoding tries to seek
            BundleManager.ConvertToImages(img, false, (newImg, _) => {
                newImg.SaveAsWebp(ms);
            }, mipLevel);
            ms.Position = 0;
            ms.WriteTo(ds);
        }
        catch {
            return False;
        }

        return True;
    }

    [UnmanagedCallersOnly, Obsolete("Do not call from managed code.", true)]
    public static int GetImageMipLevels(nint handle)
        => GetImageMipLevelsImpl(handle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetImageMipLevelsImpl(nint handle) {
        var h = new GCHandle<Image>(handle);

        if (!h.TryGetTarget(out var img)) {
            var o = h.NonGeneric.Target;
            if (o is null) return False;

            var isoCtx = GetIsolationContext(o);
            if (isoCtx == IsolationContext || isoCtx == null)
                return 0;

            Trace.TraceInformation(
                $"Dispatching to isolation context #{_GetIsolationContextId(isoCtx)} ({isoCtx.Name})");

            // redirect to the appropriate isolation context
            return IsolatedInvoke(isoCtx,
                () => GetImageMipLevelsImpl(handle));
        }

        return img.Description.MipLevels;
    }

    [UnmanagedCallersOnly, Obsolete("Do not call from managed code.", true)]
    public static int GetImageWidth(nint handle, int mipLevel)
        => GetImageWidthImpl(handle, mipLevel);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetImageWidthImpl(nint handle, int mipLevel) {
        var h = new GCHandle<Image>(handle);

        if (!h.TryGetTarget(out var img)) {
            var o = h.NonGeneric.Target;
            if (o is null) return False;

            var isoCtx = GetIsolationContext(o);
            if (isoCtx == IsolationContext || isoCtx == null)
                return -1;

            Trace.TraceInformation(
                $"Dispatching to isolation context #{_GetIsolationContextId(isoCtx)} ({isoCtx.Name})");

            // redirect to the appropriate isolation context
            return IsolatedInvoke(isoCtx,
                () => GetImageWidthImpl(handle, mipLevel));
        }

        try {
            var mip = img.GetMipMapDescription(mipLevel);
            return mip.Width;
        }
        catch {
            return -1;
        }
    }

    [UnmanagedCallersOnly, Obsolete("Do not call from managed code.", true)]
    public static int GetImageHeight(nint handle, int mipLevel)
        => GetImageHeightImpl(handle, mipLevel);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetImageHeightImpl(nint handle, int mipLevel) {
        var h = new GCHandle<Image>(handle);

        if (!h.TryGetTarget(out var img)) {
            var o = h.NonGeneric.Target;
            if (o is null) return False;

            var isoCtx = GetIsolationContext(o);
            if (isoCtx == IsolationContext || isoCtx == null)
                return -1;

            Trace.TraceInformation(
                $"Dispatching to isolation context #{_GetIsolationContextId(isoCtx)} ({isoCtx.Name})");

            // redirect to the appropriate isolation context
            return IsolatedInvoke(isoCtx,
                () => GetImageHeightImpl(handle, mipLevel));
        }

        try {
            var mip = img.GetMipMapDescription(mipLevel);
            return mip.Height;
        }
        catch {
            return -1;
        }
    }

    [UnmanagedCallersOnly, Obsolete("Do not call from managed code.", true)]
    public static int GetImageDepth(nint handle, int mipLevel)
        => GetImageDepthImpl(handle, mipLevel);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetImageDepthImpl(nint handle, int mipLevel) {
        var h = new GCHandle<Image>(handle);

        if (!h.TryGetTarget(out var img)) {
            var o = h.NonGeneric.Target;
            if (o is null) return False;

            var isoCtx = GetIsolationContext(o);
            if (isoCtx == IsolationContext || isoCtx == null)
                return -1;

            Trace.TraceInformation(
                $"Dispatching to isolation context #{_GetIsolationContextId(isoCtx)} ({isoCtx.Name})");

            // redirect to the appropriate isolation context
            return IsolatedInvoke(isoCtx,
                () => GetImageDepthImpl(handle, mipLevel));
        }

        try {
            var mip = img.GetMipMapDescription(mipLevel);
            return mip.Depth;
        }
        catch {
            return -1;
        }
    }

    [UnmanagedCallersOnly, Obsolete("Do not call from managed code.", true)]
    public static int GetImageDimensions(nint handle)
        => GetImageDimensionsImpl(handle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetImageDimensionsImpl(nint handle) {
        var h = new GCHandle<Image>(handle);

        if (!h.TryGetTarget(out var img)) {
            var o = h.NonGeneric.Target;
            if (o is null) return False;

            var isoCtx = GetIsolationContext(o);
            if (isoCtx == IsolationContext || isoCtx == null)
                return -1;

            Trace.TraceInformation(
                $"Dispatching to isolation context #{_GetIsolationContextId(isoCtx)} ({isoCtx.Name})");

            // redirect to the appropriate isolation context
            return IsolatedInvoke(isoCtx,
                () => GetImageDimensionsImpl(handle));
        }

        return img.Description.Dimension switch {
            TextureDimension.Texture1D => 1,
            TextureDimension.Texture2D => 2,
            TextureDimension.Texture3D => 3,
            TextureDimension.TextureCube => 3,
            _ => -1
        };
    }

    [ThreadStatic]
    private static (Image? Image, string? Value) _imageFormatCache;

    [UnmanagedCallersOnly, Obsolete("Do not call from managed code.", true)]
    public static unsafe int GetImageFormat(nint handle, char* pBuffer, int bufferSize)
        => GetImageFormatImpl(handle, pBuffer, bufferSize);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe int GetImageFormatImpl(nint handle, char* pBuffer, int bufferSize) {
        var h = new GCHandle<Image>(handle);

        if (!h.TryGetTarget(out var img)) {
            var o = h.NonGeneric.Target;
            if (o is null) return False;

            var isoCtx = GetIsolationContext(o);
            if (isoCtx == IsolationContext || isoCtx == null)
                return 0;

            Trace.TraceInformation(
                $"Dispatching to isolation context #{_GetIsolationContextId(isoCtx)} ({isoCtx.Name})");

            // redirect to the appropriate isolation context
            return IsolatedInvoke(isoCtx,
                () => GetImageFormatImpl(handle, pBuffer, bufferSize));
        }

        if (_imageFormatCache.Image == img)
            return ExtractUnmanagedString(_imageFormatCache.Value, pBuffer, bufferSize);

        var formatStr = img.Description.Format.ToString();
        _imageFormatCache = (img, formatStr);
        return ExtractUnmanagedString(formatStr, pBuffer, bufferSize);
    }

    [ThreadStatic]
    private static (Image? Image, string? Value) _imageTextureTypeCache;

    private static readonly Type IsoLoadCtxType = Type.GetType
        ("Internal.Runtime.InteropServices.IsolatedComponentLoadContext")!;

    [UnmanagedCallersOnly, Obsolete("Do not call from managed code.", true)]
    public static unsafe int GetImageTextureType(nint handle, char* pBuffer, int bufferSize)
        => GetImageTextureTypeImpl(handle, pBuffer, bufferSize);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe int GetImageTextureTypeImpl(nint handle, char* pBuffer, int bufferSize) {
        var h = new GCHandle<Image>(handle);

        if (!h.TryGetTarget(out var img)) {
            var o = h.NonGeneric.Target;
            if (o is null) return False;

            var isoCtx = GetIsolationContext(o);
            if (isoCtx == IsolationContext || isoCtx == null)
                return 0;

            Trace.TraceInformation(
                $"Dispatching to isolation context #{_GetIsolationContextId(isoCtx)} ({isoCtx.Name})");

            // redirect to the appropriate isolation context
            return IsolatedInvoke(isoCtx,
                () => GetImageTextureTypeImpl(handle, pBuffer, bufferSize));
        }

        if (_imageTextureTypeCache.Image == img)
            return ExtractUnmanagedString(_imageTextureTypeCache.Value, pBuffer, bufferSize);

        var formatStr = img.Description.Dimension.ToString();
        _imageTextureTypeCache = (img, formatStr);
        return ExtractUnmanagedString(formatStr, pBuffer, bufferSize);
    }

    [UnmanagedCallersOnly, Obsolete("Do not call from managed code.", true)]
    [return: DNNE.C99Type("DialogResult_t")]
    public static unsafe int ShowMessageBox(
        char* message, int messageLength,
        char* title, int titleLength,
        [DNNE.C99Type("MessageBoxButtons_t")] int messageBoxButtons,
        [DNNE.C99Type("MessageBoxType_t")] int messageBoxType)
        => ShowMessageBoxImpl(message, messageLength, title, titleLength, messageBoxButtons, messageBoxType);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe int ShowMessageBoxImpl(
        char* message, int messageLength,
        char* title, int titleLength,
        [DNNE.C99Type("MessageBoxButtons_t")] int messageBoxButtons,
        [DNNE.C99Type("MessageBoxType_t")] int messageBoxType) {
        var messageSpan = new ReadOnlySpan<char>(message, messageLength);
        var messageStr = messageSpan.IsEmpty ? null : new string(messageSpan);
        var titleSpan = new ReadOnlySpan<char>(title, titleLength);
        var titleStr = titleSpan.IsEmpty ? null : new string(titleSpan);
        var buttons = (MessageBoxButtons)messageBoxButtons;
        var type = (MessageBoxType)messageBoxType;
        if (Eto.Forms.Application.Instance is null)
            Dw2Env.Initialize();

        return (int)MessageBox.Show(messageStr, titleStr, buttons, type);
    }

    [UnmanagedCallersOnly, Obsolete("Do not call from managed code.", true)]
    [return: DNNE.C99Type("bool")]
    public static unsafe CBool TryExportObject(byte* pObjectId, char* pPath, int pathLength, int isoCtxId)
        => TryExportObjectImpl(pObjectId, pPath, pathLength, isoCtxId);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe CBool TryExportObjectImpl(byte* pObjectId, char* pPath, int pathLength, int isoCtxId) {
        if (pObjectId is null) return False;

        if (isoCtxId != IsolationContextId)
            return ((delegate*<byte*, char*, int, int, CBool>)GetExport(isoCtxId, nameof(TryExportObject)))
                (pObjectId, pPath, pathLength, isoCtxId);

        var pathSpan = new ReadOnlySpan<char>(pPath, pathLength);
        var path = pathSpan.IsEmpty ? null : new string(pathSpan);
        if (path is null) return False;

        var objectId = *(ObjectId*)pObjectId;

        return BundleManager.TryExportObject(objectId, path) ? True : False;
    }

    [UnmanagedCallersOnly, Obsolete("Do not call from managed code.", true)]
    [return: DNNE.C99Type("bool")]
    public static unsafe CBool TryExportImageAsWebp(nint handle, char* pPath, int pathLength)
        => TryExportImageAsWebpImpl(handle, pPath, pathLength);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe CBool TryExportImageAsWebpImpl(nint handle, char* pPath, int pathLength) {
        var h = new GCHandle<Image>(handle);

        if (!h.TryGetTarget(out var img)) {
            var o = h.NonGeneric.Target;
            if (o is null) return False;

            var isoCtx = GetIsolationContext(o);
            if (isoCtx == IsolationContext || isoCtx == null)
                return 0;

            Trace.TraceInformation(
                $"Dispatching to isolation context #{_GetIsolationContextId(isoCtx)} ({isoCtx.Name})");

            // redirect to the appropriate isolation context
            return IsolatedInvoke(isoCtx,
                () => TryExportImageAsWebpImpl(handle, pPath, pathLength));
        }

        var pathSpan = new ReadOnlySpan<char>(pPath, pathLength);
        var path = pathSpan.IsEmpty ? null : new string(pathSpan);
        if (path is null) return False;

        try {
            return BundleManager.TryExportImageAsWebp(img, path)
                ? True
                : False;
        }
        catch {
            return False;
        }
    }

    [UnmanagedCallersOnly, Obsolete("Do not call from managed code.", true)]
    [return: DNNE.C99Type("bool")]
    public static unsafe CBool TryExportImageAsDds(nint handle, char* pPath, int pathLength)
        => UnwrapManagedExceptions(&TryExportImageAsDdsImpl, handle, pPath, pathLength);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe CBool TryExportImageAsDdsImpl(nint handle, char* pPath, int pathLength) {
        var h = new GCHandle<Image>(handle);

        if (!h.TryGetTarget(out var img)) {
            var o = h.NonGeneric.Target;
            if (o is null) return False;

            var isoCtx = GetIsolationContext(o);
            if (isoCtx == IsolationContext || isoCtx == null)
                return 0;

            Trace.TraceInformation(
                $"Dispatching to isolation context #{_GetIsolationContextId(isoCtx)} ({isoCtx.Name})");

            // redirect to the appropriate isolation context
            return IsolatedInvoke(isoCtx,
                () => TryExportImageAsDdsImpl(handle, pPath, pathLength));
        }

        var pathSpan = new ReadOnlySpan<char>(pPath, pathLength);
        var path = pathSpan.IsEmpty ? null : new string(pathSpan);
        if (path is null) return False;

        try {
            return BundleManager.TryExportImageAsDds(img, path)
                ? True
                : False;
        }
        catch {
            return False;
        }
    }

    [UnmanagedCallersOnly, Obsolete("Do not call from managed code.", true)]
    public static unsafe nint GetLastException()
        => UnwrapManagedExceptions(&GetLastExceptionImpl);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nint GetLastExceptionImpl() {
        var ex = Interlocked.Exchange(ref LastException, null);
        if (ex is null) return 0;

        var h = new GCHandle<ExceptionDispatchInfo>(ex);

        // can use HandleToString to get details
        return h.Value;
    }

    
    public static IntPtr GetFunctionPointerFromLoadContext(AssemblyLoadContext alc,
        string typeName,
        string methodName) {
        // Create a resolver callback for types.

        // FOR DEBUGGING
        var localType = Type.GetType(typeName, false);
        var localAsm = localType?.Assembly;
        var localAsmName = localAsm?.GetName();

        // Get the requested type.
        var type = Type.GetType(typeName,
            (Func<AssemblyName, Assembly>?)alc.LoadFromAssemblyName,
            (asm, typeNameToLoad, throwOnError) => {
                if (asm is not null)
                    return asm.GetType(typeNameToLoad, throwOnError);

                foreach (var otherAsm in alc.Assemblies) {
                    var asmType = otherAsm.GetType(typeNameToLoad, false);
                    if (asmType != null)
                        return asmType;
                }

                var localType = Type.GetType(typeNameToLoad, false);
                if (localType is not null) {
                    var localAsm = localType.Assembly;
                    var localAsmName = localAsm.GetName();
                    asm = alc.LoadFromAssemblyName(localAsmName);
                    return asm.GetType(typeNameToLoad, throwOnError);
                }

                return throwOnError
                    ? throw new TypeLoadException(typeNameToLoad)
                    : null;
            }, true)!;

        // Match search semantics of the CreateDelegate() function below.
        var methodInfo = type.GetMethod(methodName,
            BindingFlags.Static
            | BindingFlags.Public
            | BindingFlags.NonPublic);
        if (methodInfo == null)
            throw new MissingMethodException(typeName, methodName);

        return methodInfo.MethodHandle.GetFunctionPointer();
    }
}