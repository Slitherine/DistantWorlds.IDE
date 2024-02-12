namespace DistantWorlds.IDE;

public sealed partial class UnmanagedDelegatedWriteOnlyStream {

    private sealed class AsyncResultImpl : AsyncResult {

        public readonly UnmanagedDelegatedWriteOnlyStream Stream;

        public readonly byte[] Buffer;

        public readonly int Offset;

        public readonly int Count;

        public AsyncResultImpl(UnmanagedDelegatedWriteOnlyStream stream,
            byte[] buffer, int offset, int count,
            AsyncCallback? callback, object? state)
            : base(callback, state) {
            Stream = stream;
            Buffer = buffer;
            Offset = offset;
            Count = count;
        }

    }

}