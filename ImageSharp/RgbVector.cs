using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp.PixelFormats;

namespace DistantWorlds.IDE.ImageSharp;

[StructLayout(LayoutKind.Sequential)]
public partial record struct RgbVector(float R, float G, float B) : IPixel<RgbVector> {

  private const float ByteMax = byte.MaxValue;

  private const float UShortMax = ushort.MaxValue;

  public static implicit operator Vector3(in RgbVector color)
    => Unsafe.As<RgbVector, Vector3>(ref Unsafe.AsRef(color));

  public static implicit operator RgbVector(in Vector3 color)
    => Unsafe.As<Vector3, RgbVector>(ref Unsafe.AsRef(color));

  public static RgbVector operator *(in RgbVector color, float scalar)
    => scalar * (Vector3)color;

  public void FromScaledVector4(Vector4 vector)
    => (R, G, B) = (vector[0], vector[1], vector[2]);

  public Vector4 ToScaledVector4()
    => new(R, G, B, 1f);

  public void FromVector4(Vector4 vector) {
    (R, G, B) = (vector[0], vector[1], vector[2]);
    this *= vector[3];
  }

  public Vector4 ToVector4()
    => new(R, G, B, 1f);

  public void FromArgb32(Argb32 source) {
    (R, G, B) = (source.R / ByteMax, source.G / ByteMax, source.B / ByteMax);
    this *= source.A;
  }

  public void FromBgra5551(Bgra5551 source)
    => FromVector4(source.ToVector4());

  public void FromBgr24(Bgr24 source)
    => (R, G, B) = (source.R / ByteMax, source.G / ByteMax, source.B / ByteMax);

  public void FromBgra32(Bgra32 source) {
    (R, G, B) = (source.R / ByteMax, source.G / ByteMax, source.B / ByteMax);
    this *= source.A / ByteMax;
  }

  public void FromAbgr32(Abgr32 source) {
    (R, G, B) = (source.R / ByteMax, source.G / ByteMax, source.B / ByteMax);
    this *= source.A / ByteMax;
  }

  public void FromL8(L8 source)
    => R = G = B = source.PackedValue / ByteMax;

  public void FromL16(L16 source)
    => R = G = B = source.PackedValue / UShortMax;

  public void FromLa16(La16 source)
    => R = G = B = source.PackedValue / UShortMax;

  public void FromLa32(La32 source)
    => R = G = B = source.L / UShortMax * (source.A / UShortMax);

  public void FromRgb24(Rgb24 source)
    => (R, G, B) = (source.R / ByteMax, source.G / ByteMax, source.B / ByteMax);

  public void FromRgba32(Rgba32 source) {
    (R, G, B) = (source.R / ByteMax, source.G / ByteMax, source.B / ByteMax);
    this *= source.A / ByteMax;
  }

  public void ToRgba32(ref Rgba32 dest)
    => dest.FromScaledVector4(ToScaledVector4());

  public void FromRgb48(Rgb48 source)
    => (R, G, B) = (source.R / UShortMax, source.G / UShortMax, source.B / UShortMax);

  public void FromRgba64(Rgba64 source) {
    (R, G, B) = (source.R / UShortMax, source.G / UShortMax, source.B / UShortMax);
    this *= source.A / UShortMax;
  }

  public PixelOperations<RgbVector> CreatePixelOperations()
    => new PixelOperations();

}