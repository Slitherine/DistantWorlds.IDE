using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace DW2IDE;

[PublicAPI]
// ReSharper disable once InconsistentNaming
public interface IGCHandle {

  GCHandle GetHandle();

}