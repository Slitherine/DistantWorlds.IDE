using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Unicode;

namespace DistantWorlds.IDE;

public static class EnumerationStream {

  public static bool Utf8StringTranslator(in string? str, Span<byte> buffer, out int bytesNeededOrWritten) {
    if (str is null) {
      bytesNeededOrWritten = 0;
      return true;
    }

    var status = Utf8.FromUtf16(str, buffer, out var read, out bytesNeededOrWritten, false);
    if (read != str.Length)
      throw new UnreachableException("Failed to read entire string.");

    switch (status) {
      case OperationStatus.Done:
        return true;

      case OperationStatus.DestinationTooSmall:
        bytesNeededOrWritten = Encoding.UTF8.GetByteCount(str);
        if (bytesNeededOrWritten > buffer.Length)
          return true;

        throw new UnreachableException("DestinationTooSmall but buffer is large enough.");

      case OperationStatus.InvalidData:
        bytesNeededOrWritten = 0;
        return false;

      case OperationStatus.NeedMoreData:
        throw new NotImplementedException("Incomplete unicode surrogate at end of string.");

      default:
        throw new UnreachableException($"Unknown OperationStatus: {status}");
    }
  }

}

public class EnumerationStream<T> : Stream {

  public delegate bool TranslatorDelegate(in T item, Span<byte> buffer, out int bytesNeededOrWritten);

  private readonly IEnumerator<T> _enumeration;

  private bool _completedEnumeration;

  private readonly TranslatorDelegate _translate;

  private byte[] _buffer;

  private int _bufferOffset;

  private int _bufferLength;

  private long _totalRead = 0;

  private Span<byte> BufferSpan
    => new(_buffer, _bufferOffset, _bufferLength - _bufferOffset);

  public EnumerationStream(IEnumerable<T> enumeration, TranslatorDelegate translator, int bufferSize = 4096) {
    _buffer = new byte[bufferSize];
    _enumeration = enumeration.GetEnumerator();
    _translate = translator;
  }

  public override void Flush() {
  }

  public override int Read(byte[] buffer, int offset, int count) {
    try {
      var read = 0;

      if (_bufferOffset < _bufferLength) {
        var span = BufferSpan;
        if (span.Length > count)
          span = span.Slice(0, count);
        span.CopyTo(buffer.AsSpan(offset, count));
        read = span.Length;
        _bufferOffset += read;
        return read;
      }
      else {
        if (_completedEnumeration)
          return 0;

        if (!_enumeration.MoveNext()) {
          _completedEnumeration = true;
          return 0;
        }

        _bufferOffset = 0;
        var success = _translate(_enumeration.Current, _buffer, out _bufferLength);
        if (!success) return 0;

        if (_bufferLength > _buffer.Length) {
          Array.Resize(ref _buffer, _bufferLength);
          success = _translate(_enumeration.Current, _buffer, out _bufferLength);
        }

        if (!success) return 0;

        var span = BufferSpan;
        if (span.Length > count)
          span = span.Slice(0, count);
        span.CopyTo(buffer.AsSpan(offset, count));
        read = span.Length;
        _bufferOffset += read;
        _totalRead += read;
        return read;
      }
    }
    catch {
      return 0;
    }
  }

  public override long Seek(long offset, SeekOrigin origin)
    => throw new NotSupportedException();

  public override void SetLength(long value)
    => throw new NotSupportedException();

  public override void Write(byte[] buffer, int offset, int count)
    => throw new NotSupportedException();

  public override bool CanRead
    => !_completedEnumeration && _bufferLength < _bufferOffset;

  public override bool CanSeek
    => false;

  public override bool CanWrite
    => false;

  public override long Length
    => throw new NotSupportedException();

  public override long Position {
    get => _totalRead;
    set => throw new NotSupportedException();
  }

}