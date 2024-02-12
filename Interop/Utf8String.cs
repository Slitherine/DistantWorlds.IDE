using System.Runtime.InteropServices;
using SharpDX.Text;
using Encoding = System.Text.Encoding;

namespace DistantWorlds.IDE.Interop;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct Utf8String {

    public byte* pBuffer;

    public nuint nBuffer;

    public Span<byte> Buffer => new(pBuffer, (int)nBuffer);

    public static implicit operator Span<byte>(Utf8String str) => str.Buffer;

    public static implicit operator ReadOnlySpan<byte>(Utf8String str) => str.Buffer;

    public const string C = //language=c++
        """
        typedef struct Utf8String {
            char* pBuffer;
            size_t nBuffer;
        } Utf8String_t;
        
        """;

    public override string? ToString() {
        if (nBuffer == 0 || pBuffer == null)
            return null;

        return Encoding.UTF8.GetString(pBuffer, (int)nBuffer);
    }

}