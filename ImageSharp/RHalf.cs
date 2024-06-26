using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp.PixelFormats;

namespace DistantWorlds.IDE.ImageSharp;

[StructLayout(LayoutKind.Sequential)]
public partial record struct RHalf(Half R) : IPixel<RHalf> {

    private const float ByteMax = byte.MaxValue;

    private const float UShortMax = ushort.MaxValue;

    public static implicit operator Half(in RHalf color)
        => Unsafe.As<RHalf, Half>(ref Unsafe.AsRef(in color));

    public static implicit operator RHalf(in Half color)
        => Unsafe.As<Half, RHalf>(ref Unsafe.AsRef(in color));

    public static implicit operator float(in RHalf color)
        => (float)(Half)color;

    public static implicit operator RHalf(in float color)
        => (Half)color;

    public static RHalf operator *(in RHalf color, float scalar)
        => scalar * (float)color;

    public void FromScaledVector4(Vector4 vector)
        => R = (Half)vector[0];

    public Vector4 ToScaledVector4()
        => new((float)R, 0, 0, 1f);

    public void FromVector4(Vector4 vector) {
        R = (Half)(vector[0]);
        this *= vector[3];
    }

    public Vector4 ToVector4()
        => new((float)R, 0, 0, 1f);

    public void FromArgb32(Argb32 source) {
        R = (Half)(source.R / ByteMax);
        this *= source.A;
    }

    public void FromBgra5551(Bgra5551 source)
        => FromVector4(source.ToVector4());

    public void FromBgr24(Bgr24 source)
        => R = (Half)(source.R / ByteMax);

    public void FromBgra32(Bgra32 source) {
        R = (Half)(source.R / ByteMax);
        this *= source.A / ByteMax;
    }

    public void FromAbgr32(Abgr32 source) {
        R = (Half)(source.R / ByteMax);
        this *= source.A / ByteMax;
    }

    public void FromL8(L8 source)
        => R = (Half)(source.PackedValue / ByteMax);

    public void FromL16(L16 source)
        => R = (Half)(source.PackedValue / UShortMax);

    public void FromLa16(La16 source)
        => R = (Half)(source.PackedValue / UShortMax);

    public void FromLa32(La32 source)
        => R = (Half)(source.L / UShortMax * (source.A / UShortMax));

    public void FromRgb24(Rgb24 source)
        => R = (Half)(source.R / ByteMax);

    public void FromRgba32(Rgba32 source) {
        R = (Half)(source.R / ByteMax);
        this *= source.A / ByteMax;
    }

    public void ToRgba32(ref Rgba32 dest)
        => dest.FromScaledVector4(ToScaledVector4());

    public void FromRgb48(Rgb48 source)
        => R = (Half)(source.R / UShortMax);

    public void FromRgba64(Rgba64 source) {
        R = (Half)(source.R / UShortMax);
        this *= source.A / UShortMax;
    }

    public PixelOperations<RHalf> CreatePixelOperations()
        => new PixelOperations();

}