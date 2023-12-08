using System;
using System.Numerics;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace DistantWorlds.IDE;

[PublicAPI]
[StructLayout(LayoutKind.Sequential)]
// ReSharper disable once InconsistentNaming
public struct GCHandle<T>
  : IDisposable,
    IGCHandle,
    IEqualityOperators<GCHandle<T>, GCHandle<T>, bool>,
    IEqualityOperators<GCHandle<T>, GCHandle, bool>,
    IEqualityOperators<GCHandle<T>, IGCHandle, bool>,
    IEquatable<GCHandle<T>>,
    IEquatable<GCHandle>,
    IEquatable<IGCHandle>
  where T : class {

  public GCHandle NonGeneric;

  private static readonly string? TypeAssemblyQualifiedName
    = typeof(T).AssemblyQualifiedName;

  [Obsolete("Use a constructor with parameters instead.", false)]
  public GCHandle()
    => NonGeneric = new();

  public GCHandle(GCHandle handle)
    => NonGeneric = handle;

  public GCHandle(T target)
    => NonGeneric = GCHandle.Alloc(target);

  public GCHandle(T target, GCHandleType type)
    => NonGeneric = GCHandle.Alloc(target, type);

  public GCHandle(nint value)
    => NonGeneric = GCHandle.FromIntPtr(value);

  public T? Target {
    readonly get => NonGeneric.IsAllocated ? (T?)NonGeneric.Target : null;
    set => NonGeneric.Target = value;
  }

  public readonly nint Value => GCHandle.ToIntPtr(NonGeneric);

  public readonly nint AddrOfPinnedObject()
    => Unsafe.AsRef(NonGeneric).AddrOfPinnedObject();

  public void Free() => NonGeneric.Free();

  void IDisposable.Dispose() => Free();

  public readonly bool IsAllocated => NonGeneric.IsAllocated;

  public readonly bool Equals(GCHandle other)
    => NonGeneric.Equals(other);

  public readonly bool Equals(GCHandle<T> other)
    => NonGeneric.Equals(other.NonGeneric);

  public readonly bool Equals(IGCHandle? other)
    => other is null
      ? !IsAllocated
      : NonGeneric.Equals(other.GetHandle());

  public readonly override bool Equals(object? obj)
    => obj switch {
      GCHandle ng => NonGeneric.Equals(ng),
      GCHandle<T> g => NonGeneric.Equals(g.NonGeneric),
      IGCHandle ig => NonGeneric.Equals(ig.GetHandle()),
      _ => false
    };

  public readonly override int GetHashCode()
    => NonGeneric.GetHashCode();

  public readonly override string ToString()
    => $"[GCHandle<{TypeAssemblyQualifiedName}> 0x{Value:X8}]";

  GCHandle IGCHandle.GetHandle()
    => NonGeneric;

  public static implicit operator GCHandle(GCHandle<T> handle) => handle.NonGeneric;

  public static implicit operator T?(GCHandle<T> handle) => handle.Target;

  public static explicit operator GCHandle<T>(GCHandle handle) => new(handle);

  public static explicit operator GCHandle<T>(T target) => new(target);

  public static bool operator ==(GCHandle<T> left, GCHandle<T> right)
    => left.Equals(right);

  public static bool operator !=(GCHandle<T> left, GCHandle<T> right)
    => !(left == right);

  public static bool operator ==(GCHandle<T> left, GCHandle right)
    => left.Equals(right);

  public static bool operator !=(GCHandle<T> left, GCHandle right)
    => !(left == right);

  public static bool operator ==(GCHandle left, GCHandle<T> right)
    => left.Equals(right);

  public static bool operator !=(GCHandle left, GCHandle<T> right)
    => !(left == right);

  public static bool operator ==(GCHandle<T> left, IGCHandle? right)
    => left.Equals(right);

  public static bool operator !=(GCHandle<T> left, IGCHandle? right)
    => !(left == right);

}