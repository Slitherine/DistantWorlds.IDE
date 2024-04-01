using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using DistantWorlds.IDE;
using FluentAssertions;

namespace Tests;

[Explicit]
public class ExportTests {

    /// <see cref="Exports.Initialize"/>
    [Test, Order(0)]
    public unsafe void Initialize() {
        //Deisolator.Initialize();
        // results in "attempted to call a UnmanagedCallersOnly method from managed code"
        // but otherwise appears to work exactly as expected.

        //var alcCount = AssemblyLoadContext.All.Count();

        var asmLoc = typeof(Dw2Env).Assembly.Location;
        var interopLoc = Path.ChangeExtension(asmLoc, ".Interop.dll");
        File.Exists(interopLoc).Should().BeTrue();
        var nativeLib = NativeLibrary.Load(interopLoc);
        nativeLib.Should().NotBeNull();
        var init = NativeLibrary.GetExport(nativeLib, "Initialize");
        init.Should().NotBe(nint.Zero);
        // attempted to call a UnmanagedCallersOnly method from managed code
        //var pfn = (delegate* unmanaged[Cdecl, SuppressGCTransition]<void>)init;
        var pfn = (delegate* unmanaged[Cdecl]<void>)init;

        pfn();

        //var alcCount2 = AssemblyLoadContext.All.Count();
        //alcCount2.Should().Be(alcCount + 1);
        var newAlc = AssemblyLoadContext.All.Where(alc => {
            string? name = alc.Name;
            if (name is null) return false;

            return name.StartsWith("IsolatedComponentLoadContext(")
                && name.EndsWith("DistantWorlds.IDE.dll)");
        }).SingleOrDefault();
        newAlc.Should().NotBeNull();

        var newAsm = newAlc!.Assemblies.SingleOrDefault(asm
            => asm.GetName().Name == "DistantWorlds.IDE");
        newAsm.Should().NotBeNull();

        var newDw2Env = newAsm!.GetType("DistantWorlds.IDE.Dw2Env");
        newDw2Env.Should().NotBeNull();
    }

    /// <see cref="Exports.GetVersion"/>
    [Test, Order(1)]
    public unsafe void GetVersion() {
        var expected = typeof(Dw2Env).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
            .InformationalVersion;
        var asmLoc = typeof(Dw2Env).Assembly.Location;
        var interopLoc = Path.ChangeExtension(asmLoc, ".Interop.dll");
        File.Exists(interopLoc).Should().BeTrue();
        var nativeLib = NativeLibrary.Load(interopLoc);
        nativeLib.Should().NotBeNull();
        var getVersion = NativeLibrary.GetExport(nativeLib, "GetVersion");
        getVersion.Should().NotBeNull();
        // attempted to call a UnmanagedCallersOnly method from managed code
        //var pfn = (delegate* unmanaged[Cdecl, SuppressGCTransition]<char*, int, int>)getVersion;
        var pfn = (delegate* unmanaged[Cdecl]<char*, int, int>)getVersion;

        var needed = pfn(null, 0);

        needed.Should().BeLessThan(0);

        var size = -needed;

        var buffer = stackalloc char[size];
        var written = pfn(buffer, size);

        written.Should().Be(size);
        buffer[written - 1].Should().Be('\0');

        var span = new ReadOnlySpan<char>(buffer, written).TrimEnd('\0');
        var actual = new string(span);
        actual.Should().Be(expected);
        
        TestContext.Out.WriteLine(actual);
    }

    /// <see cref="Exports.GetNetVersion"/>
    [Test, Order(2)]
    public unsafe void GetNetVersion() {
        var expected = typeof(object).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
            .InformationalVersion;
        var asmLoc = typeof(Dw2Env).Assembly.Location;
        var interopLoc = Path.ChangeExtension(asmLoc, ".Interop.dll");
        File.Exists(interopLoc).Should().BeTrue();
        var nativeLib = NativeLibrary.Load(interopLoc);
        nativeLib.Should().NotBeNull();
        var getVersion = NativeLibrary.GetExport(nativeLib, "GetNetVersion");
        getVersion.Should().NotBeNull();
        // attempted to call a UnmanagedCallersOnly method from managed code
        //var pfn = (delegate* unmanaged[Cdecl, SuppressGCTransition]<char*, int, int>)getVersion;
        var pfn = (delegate* unmanaged[Cdecl]<char*, int, int>)getVersion;

        var needed = pfn(null, 0);

        needed.Should().BeLessThan(0);

        var size = -needed;

        var buffer = stackalloc char[size];
        var written = pfn(buffer, size);

        written.Should().Be(size);
        buffer[written - 1].Should().Be('\0');

        var span = new ReadOnlySpan<char>(buffer, written).TrimEnd('\0');
        var actual = new string(span);
        actual.Should().Be(expected);
        
        TestContext.Out.WriteLine(actual);
    }

    /// <see cref="Exports.GetGameDirectory"/>
    [Test, Order(3)]
    public unsafe void GetGameDirectory() {
        var asmLoc = typeof(Dw2Env).Assembly.Location;
        var interopLoc = Path.ChangeExtension(asmLoc, ".Interop.dll");
        File.Exists(interopLoc).Should().BeTrue();
        var nativeLib = NativeLibrary.Load(interopLoc);
        nativeLib.Should().NotBeNull();
        var getGameDirectory = NativeLibrary.GetExport(nativeLib, "GetGameDirectory");
        getGameDirectory.Should().NotBeNull();
        // attempted to call a UnmanagedCallersOnly method from managed code
        //var pfn = (delegate* unmanaged[Cdecl, SuppressGCTransition]<char*, int, int>)getUserChosenGameDirectory;
        var pfn = (delegate* unmanaged[Cdecl]<char*, int, int>)getGameDirectory;

        var needed = pfn(null, 0);

        needed.Should().BeLessThan(0);

        var size = -needed;

        var buffer = stackalloc char[size];
        var written = pfn(buffer, size);

        written.Should().Be(size);
        buffer[written - 1].Should().Be('\0');

        var span = new ReadOnlySpan<char>(buffer, written).TrimEnd('\0');
        var actual = new string(span);
        actual.Should().NotBeNullOrWhiteSpace();
        
        TestContext.Out.WriteLine(actual);
    }
    
    /// <see cref="Exports.GetUserChosenGameDirectory"/>
    [Test, Order(4)]
    public unsafe void GetUserChosenGameDirectory() {
        var asmLoc = typeof(Dw2Env).Assembly.Location;
        var interopLoc = Path.ChangeExtension(asmLoc, ".Interop.dll");
        File.Exists(interopLoc).Should().BeTrue();
        var nativeLib = NativeLibrary.Load(interopLoc);
        nativeLib.Should().NotBeNull();
        var getUserChosenGameDirectory = NativeLibrary.GetExport(nativeLib, "GetUserChosenGameDirectory");
        getUserChosenGameDirectory.Should().NotBeNull();
        // attempted to call a UnmanagedCallersOnly method from managed code
        //var pfn = (delegate* unmanaged[Cdecl, SuppressGCTransition]<char*, int, int>)getUserChosenGameDirectory;
        var pfn = (delegate* unmanaged[Cdecl]<char*, int, int>)getUserChosenGameDirectory;

        var needed = pfn(null, 0);

        needed.Should().BeLessThan(0);

        var size = -needed;

        var buffer = stackalloc char[size];
        var written = pfn(buffer, size);

        written.Should().Be(size);
        buffer[written - 1].Should().Be('\0');

        var span = new ReadOnlySpan<char>(buffer, written).TrimEnd('\0');
        var actual = new string(span);
        actual.Should().NotBeNullOrWhiteSpace();
        
        TestContext.Out.WriteLine(actual);
    }
}