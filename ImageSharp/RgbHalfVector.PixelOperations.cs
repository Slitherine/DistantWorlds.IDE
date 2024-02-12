using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;

namespace DistantWorlds.IDE.ImageSharp;

public partial record struct RgbHalfVector {

  /// <summary>
  /// <see cref="PixelOperations{TPixel}"/> implementation optimized for <see cref="RgbaVector"/>.
  /// </summary>
  private class PixelOperations : PixelOperations<RgbHalfVector> {

    private static readonly PixelTypeInfo PixelTypeInfo
      = new(32 * 3, PixelAlphaRepresentation.None);

    /// <inheritdoc />
    public override PixelTypeInfo GetPixelTypeInfo() => PixelTypeInfo;

    /// <inheritdoc />
    public override void From<TSourcePixel>(
      Configuration configuration,
      ReadOnlySpan<TSourcePixel> sourcePixels,
      Span<RgbHalfVector> destinationPixels) {
      ref var sourceStart = ref MemoryMarshal.GetReference(sourcePixels);
      ref var sourceEnd = ref Unsafe.Add(ref sourceStart, (uint)sourcePixels.Length);
      ref var destRef = ref MemoryMarshal.GetReference(destinationPixels);

      while (Unsafe.IsAddressLessThan(ref sourceStart, ref sourceEnd)) {
        var v4 = sourceStart.ToVector4().AsVector128();
        destRef = v4.AsVector3() * v4[3];

        sourceStart = ref Unsafe.Add(ref sourceStart, 1);
        destRef = ref Unsafe.Add(ref destRef, 1);
      }
    }

    /// <inheritdoc />
    public override void FromVector4Destructive(
      Configuration configuration,
      Span<Vector4> sourceVectors,
      Span<RgbHalfVector> destinationPixels,
      PixelConversionModifiers modifiers) {
      // unpremultiply, srgb compand
      if ((modifiers & PixelConversionModifiers.Premultiply) != 0)
        throw new NotImplementedException();
      if ((modifiers & PixelConversionModifiers.SRgbCompand) != 0)
        throw new NotImplementedException();

      ref var sourceStart = ref MemoryMarshal.GetReference(sourceVectors);
      ref var sourceEnd = ref Unsafe.Add(ref sourceStart, (uint)sourceVectors.Length);
      ref var destRef = ref MemoryMarshal.GetReference(destinationPixels);

      while (Unsafe.IsAddressLessThan(ref sourceStart, ref sourceEnd)) {
        var v4 = sourceStart.AsVector128();
        destRef = v4.AsVector3() * v4[3];

        sourceStart = ref Unsafe.Add(ref sourceStart, 1);
        destRef = ref Unsafe.Add(ref destRef, 1);
      }
    }

    /// <inheritdoc />
    public override void ToVector4(
      Configuration configuration,
      ReadOnlySpan<RgbHalfVector> sourcePixels,
      Span<Vector4> destinationVectors,
      PixelConversionModifiers modifiers) {
      // unpremultiply, srgb compand
      if ((modifiers & PixelConversionModifiers.Premultiply) != 0)
        throw new NotImplementedException();
      if ((modifiers & PixelConversionModifiers.SRgbCompand) != 0)
        throw new NotImplementedException();

      ref var sourceStart = ref MemoryMarshal.GetReference(sourcePixels);
      ref var sourceEnd = ref Unsafe.Add(ref sourceStart, (uint)sourcePixels.Length);
      ref var destRef = ref MemoryMarshal.GetReference(destinationVectors);

      while (Unsafe.IsAddressLessThan(ref sourceStart, ref sourceEnd)) {
        destRef = sourceStart.ToVector4();

        sourceStart = ref Unsafe.Add(ref sourceStart, 1);
        destRef = ref Unsafe.Add(ref destRef, 1);
      }
    }

    public override void ToL8(
      Configuration configuration,
      ReadOnlySpan<RgbHalfVector> sourcePixels,
      Span<L8> destinationPixels) {
      ref var sourceBaseRef = ref MemoryMarshal.GetReference(sourcePixels);
      ref var destBaseRef = ref MemoryMarshal.GetReference(destinationPixels);

      for (nuint i = 0; i < (uint)sourcePixels.Length; i++) {
        ref var sp = ref Unsafe.Add(ref sourceBaseRef, i);
        ref var dp = ref Unsafe.Add(ref destBaseRef, i);
        dp.FromVector4(sp.ToVector4());
      }
    }

    public override void ToL16(
      Configuration configuration,
      ReadOnlySpan<RgbHalfVector> sourcePixels,
      Span<L16> destinationPixels) {
      ref var sourceBaseRef = ref MemoryMarshal.GetReference(sourcePixels);
      ref var destBaseRef = ref MemoryMarshal.GetReference(destinationPixels);

      for (nuint i = 0; i < (uint)sourcePixels.Length; i++) {
        ref var sp = ref Unsafe.Add(ref sourceBaseRef, i);
        ref var dp = ref Unsafe.Add(ref destBaseRef, i);
        dp.FromVector4(sp.ToVector4());
      }
    }

  }

}