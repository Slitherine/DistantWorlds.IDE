using System.Runtime.InteropServices;

namespace DistantWorlds.IDE.Interop;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct Utf16String {

    public char* pBuffer;

    public nuint nBuffer;

    public Span<char> Buffer => new(pBuffer, (int)nBuffer);

    public static implicit operator Span<char>(Utf16String str) => str.Buffer;

    public static implicit operator ReadOnlySpan<char>(Utf16String str) => str.Buffer;

    public const string C = //language=c++
        """
        typedef struct Utf16String {
            DNNE_WCHAR* pBuffer;
            size_t nBuffer;
        } Utf16String_t;
        
        """;

    public override string? ToString() {
        if (nBuffer == 0 || pBuffer == null)
            return null;

        return new(pBuffer, 0, (int)nBuffer);
    }

}