using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using MonoMod.RuntimeDetour;

namespace DistantWorlds.IDE;

public static class Deisolator {

    private const nint AllBitsNativeInt = -1;

    private const BindingFlags BindingFlagsStaticMember
        = BindingFlags.Static
        | BindingFlags.Public
        | BindingFlags.NonPublic;

    [SuppressMessage("ReSharper", "CollectionNeverQueried.Local",
        Justification = "Anti-GC measure, otherwise they'd be collected immediately.")]
    private static readonly Dictionary<IntPtr, Delegate> GeneratedDelegates = new();

    private static readonly AssemblyLoadContext OurAssemblyLoadContext =
        AssemblyLoadContext.GetLoadContext(typeof(Deisolator).Assembly)!;

    private static readonly Hook LoadAssemblyAndGetFunctionPointerHook;

    static unsafe Deisolator() {
        var t = Type.GetType("Internal.Runtime.InteropServices.ComponentActivator");
        var m = t!.GetMethod("LoadAssemblyAndGetFunctionPointer",
            BindingFlagsStaticMember);
        LoadAssemblyAndGetFunctionPointerHook = new(m!, LoadAssemblyAndGetFunctionPointer);
    }

    private static string? MarshalToString(nint ptr, string name)
        => ptr == IntPtr.Zero
            ? throw new ArgumentNullException(name)
            : Marshal.PtrToStringAuto(ptr)!;

    //[UnmanagedCallersOnly]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static unsafe int LoadAssemblyAndGetFunctionPointer(IntPtr assemblyPathNative,
        IntPtr typeNameNative,
        IntPtr methodNameNative,
        IntPtr delegateTypeNative,
        IntPtr reserved,
        IntPtr functionHandle) {
        try {
            var assemblyPath = MarshalToString(assemblyPathNative, nameof(assemblyPathNative));
            ArgumentNullException.ThrowIfNull(assemblyPath);
            var typeName = MarshalToString(typeNameNative, nameof(typeNameNative));
            ArgumentNullException.ThrowIfNull(typeName);
            var methodName = MarshalToString(methodNameNative, nameof(methodNameNative));
            ArgumentNullException.ThrowIfNull(methodName);

            ArgumentOutOfRangeException.ThrowIfNotEqual(reserved, IntPtr.Zero);
            ArgumentNullException.ThrowIfNull(functionHandle);

            // Set up the AssemblyLoadContext for this delegate.
            var asm = OurAssemblyLoadContext.Assemblies.FirstOrDefault(asm => asm.Location == assemblyPath)
                ?? OurAssemblyLoadContext.LoadFromAssemblyPath(assemblyPath);

            // Create the function pointer.
            *(IntPtr*)functionHandle =
                InternalGetFunctionPointer(OurAssemblyLoadContext, typeName, methodName, delegateTypeNative);
        }
        catch (Exception e) {
            return e.HResult;
        }

        return 0;
    }

    public delegate int ComponentEntryPoint(IntPtr args, int sizeBytes);

    private static IntPtr InternalGetFunctionPointer(AssemblyLoadContext alc,
        string typeName,
        string methodName,
        IntPtr delegateTypeNative) {
        // Create a resolver callback for types.
        var resolver = alc.LoadFromAssemblyName;

        // Determine the signature of the type. There are 3 possibilities:
        //  * No delegate type was supplied - use the default (i.e. ComponentEntryPoint).
        //  * A sentinel value was supplied - the function is marked UnmanagedCallersOnly. This means
        //      a function pointer can be returned without creating a delegate.
        //  * A delegate type was supplied - Load the type and create a delegate for that method.
        Type? delegateType;
        if (delegateTypeNative == IntPtr.Zero) {
            delegateType = typeof(ComponentEntryPoint);
        }
        else if (delegateTypeNative == AllBitsNativeInt) {
            delegateType = null;
        }
        else {
            var delegateTypeName = MarshalToString(delegateTypeNative, nameof(delegateTypeNative));
            ArgumentNullException.ThrowIfNull(delegateTypeName);
            delegateType = Type.GetType(delegateTypeName, resolver, null, throwOnError: true)!;
        }

        // Get the requested type.
        var type = Type.GetType(typeName, resolver, null, throwOnError: true)!;

        IntPtr functionPtr;
        if (delegateType == null) {
            // Match search semantics of the CreateDelegate() function below.
            var methodInfo = type.GetMethod(methodName, BindingFlagsStaticMember);
            if (methodInfo == null)
                throw new MissingMethodException(typeName, methodName);

            // Verify the function is properly marked.
            if (null == methodInfo.GetCustomAttribute<UnmanagedCallersOnlyAttribute>())
                throw new InvalidOperationException("Function is missing the UnmanagedCallersOnly attribute.");

            // workaround for ExecutionEngineException
            // Fatal error. Invalid Program: attempted to call a UnmanagedCallersOnly method from managed code.
            
            functionPtr = methodInfo.MethodHandle.GetFunctionPointer();
        }
        else {
            var d = Delegate.CreateDelegate(delegateType, type, methodName)!;

            functionPtr = Marshal.GetFunctionPointerForDelegate(d);

            lock (GeneratedDelegates) {
                // Keep a reference to the delegate to prevent it from being garbage collected
                GeneratedDelegates[functionPtr] = d;
            }
        }

        return functionPtr;
    }

    public static void Initialize() {
        // just to trigger the static constructor
    }

}