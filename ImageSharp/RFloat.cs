using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp.PixelFormats;

namespace DistantWorlds.IDE.ImageSharp;

[StructLayout(LayoutKind.Sequential)]
public partial record struct RFloat(float R) : IPixel<RFloat> {

  private const float ByteMax = byte.MaxValue;

  private const float UShortMax = ushort.MaxValue;

  public static implicit operator float(in RFloat color)
    => Unsafe.As<RFloat, float>(ref Unsafe.AsRef(color));

  public static implicit operator RFloat(in float color)
    => Unsafe.As<float, RFloat>(ref Unsafe.AsRef(color));

  public static RFloat operator *(in RFloat color, float scalar)
    => scalar * (float)color;

  public void FromScaledVector4(Vector4 vector)
    => R = vector[0];

  public Vector4 ToScaledVector4()
    => new(R, 0, 0, 1f);

  public void FromVector4(Vector4 vector) {
    R = (vector[0]);
    this *= vector[3];
  }

  public Vector4 ToVector4()
    => new(R, 0, 0, 1f);

  public void FromArgb32(Argb32 source) {
    R = (source.R / ByteMax);
    this *= source.A;
  }

  public void FromBgra5551(Bgra5551 source)
    => FromVector4(source.ToVector4());

  public void FromBgr24(Bgr24 source)
    => R = (source.R / ByteMax);

  public void FromBgra32(Bgra32 source) {
    R = (source.R / ByteMax);
    this *= source.A / ByteMax;
  }

  public void FromAbgr32(Abgr32 source) {
    R = (source.R / ByteMax);
    this *= source.A / ByteMax;
  }

  public void FromL8(L8 source)
    => R = source.PackedValue / ByteMax;

  public void FromL16(L16 source)
    => R = source.PackedValue / UShortMax;

  public void FromLa16(La16 source)
    => R = source.PackedValue / UShortMax;

  public void FromLa32(La32 source)
    => R = source.L / UShortMax * (source.A / UShortMax);

  public void FromRgb24(Rgb24 source)
    => R = source.R / ByteMax;

  public void FromRgba32(Rgba32 source) {
    R = source.R / ByteMax;
    this *= source.A / ByteMax;
  }

  public void ToRgba32(ref Rgba32 dest)
    => dest.FromScaledVector4(ToScaledVector4());

  public void FromRgb48(Rgb48 source)
    => R = source.R / UShortMax;

  public void FromRgba64(Rgba64 source) {
    R = source.R / UShortMax;
    this *= source.A / UShortMax;
  }

  public PixelOperations<RFloat> CreatePixelOperations()
    => new PixelOperations();

}