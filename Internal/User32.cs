using System.Runtime.InteropServices;

namespace DistantWorlds.IDE;

internal class User32 {

  private const string Lib = "User32";
  
  [DllImport(Lib, EntryPoint = "SetProcessDpiAwarenessContext", SetLastError = true)]
  internal static extern bool SetProcessDpiAwarenessContext(DpiAwarenessContext dpiFlag);

}