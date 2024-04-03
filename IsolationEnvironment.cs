using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace DistantWorlds.IDE;

[SkipLocalsInit]
public static class IsolationEnvironment {

    public static readonly AssemblyLoadContext Current
        = AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly())!;

    public static readonly bool IsDefault
        = AssemblyLoadContext.Default
        == Current;

    private static readonly Type IsoLoadCtxType = Type.GetType
        ("Internal.Runtime.InteropServices.IsolatedComponentLoadContext")!;

    public static AssemblyLoadContext Create(string asmLocation)
        => (AssemblyLoadContext)Activator.CreateInstance(IsoLoadCtxType, asmLocation)!;

}