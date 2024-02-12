using System.Runtime.InteropServices;

namespace DistantWorlds.IDE.Interop;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct Array<T> where T : unmanaged {

    public T* pItems;

    public nint nItems;

    public Span<T> Items => new(pItems, (int)nItems);

    public static implicit operator Span<T>(Array<T> array) => array.Items;

    public static implicit operator ReadOnlySpan<T>(Array<T> array) => array.Items;

}

public static class Array {

    public const string C = //language=c++
        """
        #ifdef __cplusplus
        template<typename T = void>
        struct Array {
            T* pItems;
            intptr_t nItems;
        };
        typedef Array<> Array_t;
        #else
        typedef struct Array {
            void* pItems;
            intptr_t nItems;
        } Array_t;
        #endif
        
        """;

}