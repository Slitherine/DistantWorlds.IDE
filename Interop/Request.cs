using System.Runtime.InteropServices;

namespace DistantWorlds.IDE.Interop;

[StructLayout(LayoutKind.Sequential)]
public struct Request {

    public Utf16String Method;

    public Array<Utf16String> Headers;

    public Array<byte> Content;

    // ReadContent must be called if set, even if Content is also set
    // if Content is set, data returned should be appended
    // it should continue to be called until it returns 0
    public unsafe delegate * unmanaged[Cdecl]<
        void* /*state*/,
        byte* /*buffer*/,
        nint /*bufferSize*/,
        nint /*return: written*/
        > ReadContent;

    public unsafe void* ReadContentState;

    public const string C = //language=c++
        """
        #ifdef __cplusplus
        typedef struct Request {
            Utf16String Method;
            Array<Utf16String> Headers;
            Array<unsigned char> Content;
            intptr_t (*ReadContent)(void* state, unsigned char* buffer, intptr_t bufferSize);
            void* ReadContentState;
        } Request_t;
        #else
        typedef struct Request {
            Utf16String_t Method;
            Array_t Headers;
            Array_t Content;
            intptr_t (*ReadContent)(void* state, unsigned char* buffer, intptr_t bufferSize);
            void* ReadContentState;
        } Request_t;
        #endif
        
        """;

}