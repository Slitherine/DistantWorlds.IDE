using System.Runtime.InteropServices;

namespace DistantWorlds.IDE;

internal partial class User32 {

  private const string Lib = "User32";
  
  [LibraryImport(Lib, EntryPoint = "SetProcessDpiAwarenessContext", SetLastError = true)]
  [return: MarshalAs(UnmanagedType.I4)]
  internal static partial bool SetProcessDpiAwarenessContext(DpiAwarenessContext dpiFlag);

}