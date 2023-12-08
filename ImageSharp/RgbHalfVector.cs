using System;
using System.Numerics;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp.PixelFormats;

namespace DistantWorlds.IDE.ImageSharp;

[StructLayout(LayoutKind.Sequential)]
public partial record struct RgbHalfVector(Half R, Half G, Half B) : IPixel<RgbHalfVector> {

  private const float ByteMax = byte.MaxValue;

  private const float UShortMax = ushort.MaxValue;

  public static implicit operator Vector3(in RgbHalfVector color)
    => new((float)color.R, (float)color.G, (float)color.B);

  public static implicit operator RgbHalfVector(in Vector3 color)
    => new((Half)color.X, (Half)color.Y, (Half)color.Z);

  public static RgbHalfVector operator *(in RgbHalfVector color, float scalar)
    => scalar * (Vector3)color;

  public void FromScaledVector4(Vector4 vector)
    => (R, G, B) = ((Half)vector[0], (Half)vector[1], (Half)vector[2]);

  public Vector4 ToScaledVector4()
    => new((float)R, (float)G, (float)B, 1f);

  public void FromVector4(Vector4 vector) {
    (R, G, B) = ((Half)vector[0], (Half)vector[1], (Half)vector[2]);
    this *= vector[3];
  }

  public Vector4 ToVector4()
    => new((float)R, (float)G, (float)B, 1f);

  public void FromArgb32(Argb32 source) {
    (R, G, B) = ((Half)(source.R / ByteMax), (Half)(source.G / ByteMax), (Half)(source.B / ByteMax));
    this *= source.A;
  }

  public void FromBgra5551(Bgra5551 source)
    => FromVector4(source.ToVector4());

  public void FromBgr24(Bgr24 source)
    => (R, G, B) = ((Half)(source.R / ByteMax), (Half)(source.G / ByteMax), (Half)(source.B / ByteMax));

  public void FromBgra32(Bgra32 source) {
    (R, G, B) = ((Half)(source.R / ByteMax), (Half)(source.G / ByteMax), (Half)(source.B / ByteMax));
    this *= source.A / ByteMax;
  }

  public void FromAbgr32(Abgr32 source) {
    (R, G, B) = ((Half)(source.R / ByteMax), (Half)(source.G / ByteMax), (Half)(source.B / ByteMax));
    this *= source.A / ByteMax;
  }

  public void FromL8(L8 source)
    => R = G = B = (Half)(source.PackedValue / ByteMax);

  public void FromL16(L16 source)
    => R = G = B = (Half)(source.PackedValue / UShortMax);

  public void FromLa16(La16 source)
    => R = G = B = (Half)(source.PackedValue / UShortMax);

  public void FromLa32(La32 source)
    => R = G = B = (Half)(source.L / UShortMax * (source.A / UShortMax));

  public void FromRgb24(Rgb24 source)
    => (R, G, B) = ((Half)(source.R / ByteMax), (Half)(source.G / ByteMax), (Half)(source.B / ByteMax));

  public void FromRgba32(Rgba32 source) {
    (R, G, B) = ((Half)(source.R / ByteMax), (Half)(source.G / ByteMax), (Half)(source.B / ByteMax));
    this *= source.A / ByteMax;
  }

  public void ToRgba32(ref Rgba32 dest)
    => dest.FromScaledVector4(ToScaledVector4());

  public void FromRgb48(Rgb48 source)
    => (R, G, B) = ((Half)(source.R / UShortMax), (Half)(source.G / UShortMax), (Half)(source.B / UShortMax));

  public void FromRgba64(Rgba64 source) {
    (R, G, B) = ((Half)(source.R / UShortMax), (Half)(source.G / UShortMax), (Half)(source.B / UShortMax));
    this *= source.A / UShortMax;
  }

  public PixelOperations<RgbHalfVector> CreatePixelOperations()
    => new PixelOperations();

}