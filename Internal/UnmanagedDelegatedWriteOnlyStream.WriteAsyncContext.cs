namespace DistantWorlds.IDE;

public sealed partial class UnmanagedDelegatedWriteOnlyStream {

    private class WriteAsyncContext {

        public readonly TaskCompletionSource CompletionSource = new();

        public readonly UnmanagedDelegatedWriteOnlyStream Stream;

        public readonly byte[] Buffer;

        public readonly int Offset;

        public readonly int Count;

        public readonly CancellationToken CancellationToken;

        public WriteAsyncContext(UnmanagedDelegatedWriteOnlyStream stream, byte[] buffer,
            int offset, int count, CancellationToken cancellationToken) {
            Stream = stream;
            Buffer = buffer;
            Offset = offset;
            Count = count;
            CancellationToken = cancellationToken;
        }

    }

}