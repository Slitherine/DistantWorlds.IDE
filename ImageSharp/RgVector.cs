using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp.PixelFormats;

namespace DistantWorlds.IDE.ImageSharp;

[StructLayout(LayoutKind.Sequential)]
public partial record struct RgVector(float R, float G) : IPixel<RgVector> {

  private const float ByteMax = byte.MaxValue;

  private const float UShortMax = ushort.MaxValue;

  public static implicit operator Vector2(in RgVector color)
    => Unsafe.As<RgVector, Vector2>(ref Unsafe.AsRef(in color));

  public static implicit operator RgVector(in Vector2 color)
    => Unsafe.As<Vector2, RgVector>(ref Unsafe.AsRef(in color));

  public static RgVector operator *(in RgVector color, float scalar)
    => scalar * (Vector2)color;

  public void FromScaledVector4(Vector4 vector)
    => (R, G) = (vector[0], vector[1]);

  public Vector4 ToScaledVector4()
    => new(R, G, 0, 1f);

  public void FromVector4(Vector4 vector) {
    (R, G) = (vector[0], vector[1]);
    this *= vector[3];
  }

  public Vector4 ToVector4()
    => new(R, G, 0, 1f);

  public void FromArgb32(Argb32 source) {
    (R, G) = (source.R / ByteMax, source.G / ByteMax);
    this *= source.A;
  }

  public void FromBgra5551(Bgra5551 source)
    => FromVector4(source.ToVector4());

  public void FromBgr24(Bgr24 source)
    => (R, G) = (source.R / ByteMax, source.G / ByteMax);

  public void FromBgra32(Bgra32 source) {
    (R, G) = (source.R / ByteMax, source.G / ByteMax);
    this *= source.A / ByteMax;
  }

  public void FromAbgr32(Abgr32 source) {
    (R, G) = (source.R / ByteMax, source.G / ByteMax);
    this *= source.A / ByteMax;
  }

  public void FromL8(L8 source)
    => R = G = source.PackedValue / ByteMax;

  public void FromL16(L16 source)
    => R = G = source.PackedValue / UShortMax;

  public void FromLa16(La16 source)
    => R = G = source.PackedValue / UShortMax;

  public void FromLa32(La32 source)
    => R = G = source.L / UShortMax * (source.A / UShortMax);

  public void FromRgb24(Rgb24 source)
    => (R, G) = (source.R / ByteMax, source.G / ByteMax);

  public void FromRgba32(Rgba32 source) {
    (R, G) = (source.R / ByteMax, source.G / ByteMax);
    this *= source.A / ByteMax;
  }

  public void ToRgba32(ref Rgba32 dest)
    => dest.FromScaledVector4(ToScaledVector4());

  public void FromRgb48(Rgb48 source)
    => (R, G) = (source.R / UShortMax, source.G / UShortMax);

  public void FromRgba64(Rgba64 source) {
    (R, G) = (source.R / UShortMax, source.G / UShortMax);
    this *= source.A / UShortMax;
  }

  public PixelOperations<RgVector> CreatePixelOperations()
    => new PixelOperations();

}