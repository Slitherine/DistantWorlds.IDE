using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace DistantWorlds.IDE;

[PublicAPI]
// ReSharper disable once InconsistentNaming
public interface IGCHandle {

  GCHandle GetHandle();

}