using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DistantWorlds.IDE;

public static class Internals {

  /*
  [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ReflectionSerialization_GetUninitializedObject")]
  private static partial void GetUninitializedObject(QCallTypeHandle type, ObjectHandleOnStack retObject);
   */

  private static unsafe delegate *<QCallTypeHandle, ObjectHandleOnStack, void> _GetUninitializedObject
    = (delegate *<QCallTypeHandle, ObjectHandleOnStack, void>)
    typeof(RuntimeHelpers).GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
      .First(mi => {
        if (nameof(GetUninitializedObject) != mi.Name)
          return false;
        if (mi.ReturnType != typeof(void))
          return false;

        var paramInfos = mi.GetParameters();
        if (paramInfos.Length != 2)
          return false;

        if (paramInfos[0].ParameterType.Name != "QCallTypeHandle")
          return false;
        if (paramInfos[1].ParameterType.Name != "ObjectHandleOnStack")
          return false;

        return true;
      }).MethodHandle.GetFunctionPointer();

  private static unsafe object GetUninitializedObject(QCallTypeHandle typeHandle) {
    object? obj = null;
    Debug.Assert(_GetUninitializedObject is not null);
    _GetUninitializedObject(typeHandle, ObjectHandleOnStack.Create(ref obj));
    return obj!;
  }

  public static Func<T> GetUninitializedObjectFactory<T>() {
    var rth = RuntimeTypeHandle.FromIntPtr(typeof(T).GetTypeInfo().TypeHandle.Value);
    return () => (T)GetUninitializedObject(new(ref rth));
  }

}

internal unsafe ref struct QCallTypeHandle {

  private void* _ptr;

  private nint _handle;

  internal QCallTypeHandle(ref RuntimeTypeHandle rth) {
    _ptr = Unsafe.AsPointer(ref rth);
    _handle = rth.Value;
  }

}

internal unsafe ref struct ObjectHandleOnStack {

  private void* _ptr;

  private ObjectHandleOnStack(void* pObject)
    => _ptr = pObject;

  internal static ObjectHandleOnStack Create<T>(ref T o) where T : class?
    => new(Unsafe.AsPointer(ref o));

}