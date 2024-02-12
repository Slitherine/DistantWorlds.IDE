using System.Numerics;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp.PixelFormats;

namespace DistantWorlds.IDE.ImageSharp;

[StructLayout(LayoutKind.Sequential)]
public partial record struct RgHalfVector(Half R, Half G) : IPixel<RgHalfVector> {

  private const float ByteMax = byte.MaxValue;

  private const float UShortMax = ushort.MaxValue;

  public static implicit operator Vector2(in RgHalfVector color)
    => new((float)color.R,(float)color.G );

  public static implicit operator RgHalfVector(in Vector2 color)
    => new((Half)color.X, (Half)color.Y);

  public static RgHalfVector operator *(in RgHalfVector color, float scalar)
    => scalar * (Vector2)color;

  public void FromScaledVector4(Vector4 vector)
    => (R, G) = ((Half)vector[0], (Half)vector[1]);

  public Vector4 ToScaledVector4()
    => new((float)R, (float)G, 0, 1f);

  public void FromVector4(Vector4 vector) {
    (R, G) = ((Half)vector[0], (Half)vector[1]);
    this *= vector[3];
  }

  public Vector4 ToVector4()
    => new((float)R, (float)G, 0, 1f);

  public void FromArgb32(Argb32 source) {
    (R, G) = ((Half)(source.R / ByteMax), (Half)(source.G / ByteMax));
    this *= source.A;
  }

  public void FromBgra5551(Bgra5551 source)
    => FromVector4(source.ToVector4());

  public void FromBgr24(Bgr24 source)
    => (R, G) = ((Half)(source.R / ByteMax), (Half)(source.G / ByteMax));

  public void FromBgra32(Bgra32 source) {
    (R, G) = ((Half)(source.R / ByteMax), (Half)(source.G / ByteMax));
    this *= source.A / ByteMax;
  }

  public void FromAbgr32(Abgr32 source) {
    (R, G) = ((Half)(source.R / ByteMax), (Half)(source.G / ByteMax));
    this *= source.A / ByteMax;
  }

  public void FromL8(L8 source)
    => R = G = (Half)(source.PackedValue / ByteMax);

  public void FromL16(L16 source)
    => R = G = (Half)(source.PackedValue / UShortMax);

  public void FromLa16(La16 source)
    => R = G = (Half)(source.PackedValue / UShortMax);

  public void FromLa32(La32 source)
    => R = G = (Half)(source.L / UShortMax * (source.A / UShortMax));

  public void FromRgb24(Rgb24 source)
    => (R, G) = ((Half)(source.R / ByteMax), (Half)(source.G / ByteMax));

  public void FromRgba32(Rgba32 source) {
    (R, G) = ((Half)(source.R / ByteMax), (Half)(source.G / ByteMax));
    this *= source.A / ByteMax;
  }

  public void ToRgba32(ref Rgba32 dest)
    => dest.FromScaledVector4(ToScaledVector4());

  public void FromRgb48(Rgb48 source)
    => (R, G) = ((Half)(source.R / ByteMax), (Half)(source.G / ByteMax));

  public void FromRgba64(Rgba64 source) {
    (R, G) = ((Half)(source.R / ByteMax), (Half)(source.G / ByteMax));
    this *= source.A / UShortMax;
  }

  public PixelOperations<RgHalfVector> CreatePixelOperations()
    => new PixelOperations();

}