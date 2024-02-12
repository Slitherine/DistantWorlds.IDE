using System.Runtime.InteropServices;

namespace DistantWorlds.IDE.Interop;

[StructLayout(LayoutKind.Sequential)]
public struct Response {

    public int StatusCode;

    public Array<Utf16String> Headers;

    public Array<byte> Content;

    // ReadContent is always called if set, even if Content is also set
    // if Content is set, data returned will be appended
    // it will continue to be called until it returns 0
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
        typedef struct Response {
            int StatusCode;
            Utf16String StatusText;
            Array<Utf16String> Headers;
            Array<unsigned char> Content;
            intptr_t (*ReadContent)(void* state, unsigned char* buffer, intptr_t bufferSize);
            void* ReadContentState;
        } Response_t;
        #else
        typedef struct Response {
            int StatusCode;
            Utf16String_t StatusText;
            Array_t Headers;
            Array_t Content;
            intptr_t (*ReadContent)(void* state, unsigned char* buffer, intptr_t bufferSize);
            void* ReadContentState;
        } Response;
        #endif
        
        """;

}