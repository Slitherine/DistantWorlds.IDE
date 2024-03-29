using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace DistantWorlds.IDE;

[PublicAPI]
public readonly struct Ptr<T> where T : unmanaged {

    public readonly unsafe T* Value;

    // ReSharper disable once ConvertToPrimaryConstructor
    public unsafe Ptr(T* value)
        => Value = value;

    public unsafe Ptr(in T value)
        => Value = (T*)Unsafe.AsPointer(ref Unsafe.AsRef(in value));

    public static unsafe implicit operator Ptr<T>(T* value)
        => new(value);

    public static unsafe implicit operator T*(Ptr<T> value)
        => value.Value;

    public static explicit operator Ptr<T>(in T value)
        => new(value);

    public static unsafe explicit operator T(Ptr<T> value)
        => *value.Value;

}