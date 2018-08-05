using System;
using System.Numerics;
using OpenTK.Graphics;
using SixLabors.ImageSharp.PixelFormats;

namespace SS14.Noise
{
    public static class Util
    {
        public static Color4 ColorFromHex(string hex)
        {
            var hstyle = System.Globalization.NumberStyles.HexNumber;
            var r = (byte)int.Parse(hex.Substring(1, 2), hstyle);
            var g = (byte)int.Parse(hex.Substring(3, 2), hstyle);
            var b = (byte)int.Parse(hex.Substring(5, 2), hstyle);
            var a = (byte)255;
            if (hex.Length == 9)
            {
                a = (byte)int.Parse(hex.Substring(7, 2), hstyle);
            }

            return new Color4(r, g, b, a);
        }

        /// <summary>
        ///     Equivalent to GLSL's mix().
        /// </summary>
        public static Color4 ColorMix(Color4 x, Color4 y, float bias)
        {
            return new Color4(x.R * (1 - bias) + y.R * bias, x.G * (1 - bias) + y.G * bias, x.B * (1 - bias) + y.B * bias, x.A * (1 - bias) + y.A * bias);
        }

        public static Color4 Blend(Color4 dstColor, Color4 srcColor, BlendFactor dstFactor, BlendFactor srcFactor)
        {
            var dst = new Vector3(dstColor.R, dstColor.G, dstColor.B);
            var src = new Vector3(srcColor.R, srcColor.G, srcColor.B);

            var ret = new Vector3();

            switch (dstFactor)
            {
                case BlendFactor.Zero:
                    break;
                case BlendFactor.One:
                    ret += dst;
                    break;
                case BlendFactor.SrcColor:
                    ret += dst * src;
                    break;
                case BlendFactor.OneMinusSrcColor:
                    ret += dst * (Vector3.One - src);
                    break;
                case BlendFactor.DstColor:
                    ret += dst * dst;
                    break;
                case BlendFactor.OneMinusDstColor:
                    ret += dst * (Vector3.One - dst);
                    break;
                case BlendFactor.SrcAlpha:
                    ret += dst * srcColor.A;
                    break;
                case BlendFactor.OneMinusSrcAlpha:
                    ret += dst * (1 - srcColor.A);
                    break;
                case BlendFactor.DstAlpha:
                    ret += dst * dstColor.A;
                    break;
                case BlendFactor.OneMinusDstAlpha:
                    ret += dst * (1 - dstColor.A);
                    break;
                default:
                    throw new NotImplementedException();
            }

            switch (srcFactor)
            {
                case BlendFactor.Zero:
                    break;
                case BlendFactor.One:
                    ret += src;
                    break;
                case BlendFactor.SrcColor:
                    ret += src * src;
                    break;
                case BlendFactor.OneMinusSrcColor:
                    ret += src * (Vector3.One - src);
                    break;
                case BlendFactor.DstColor:
                    ret += src * dst;
                    break;
                case BlendFactor.OneMinusDstColor:
                    ret += src * (Vector3.One - dst);
                    break;
                case BlendFactor.SrcAlpha:
                    ret += src * srcColor.A;
                    break;
                case BlendFactor.OneMinusSrcAlpha:
                    ret += src * (1 - srcColor.A);
                    break;
                case BlendFactor.DstAlpha:
                    ret += src * dstColor.A;
                    break;
                case BlendFactor.OneMinusDstAlpha:
                    ret += src * (1 - dstColor.A);
                    break;
                default:
                    throw new NotImplementedException();
            }

            // TODO: maybe setting alpha to 1 here is bad.
            // Dunno.
            return new Color4(ret.X, ret.Y, ret.Z, 1);
        }

        public static Color4 Convert(this Rgba32 color)
        {
            return new Color4(color.R, color.G, color.B, color.A);
        }

        public static Rgba32 Convert(this Color4 color)
        {
            return new Rgba32(color.R, color.G, color.B, color.A);
        }

        public static int SaneMod(int x, int m)
        {
            return (x % m + m) % m;
        }
    }

    public enum BlendFactor
    {
        Zero,
        One,
        SrcColor,
        OneMinusSrcColor,
        DstColor,
        OneMinusDstColor,
        SrcAlpha,
        OneMinusSrcAlpha,
        DstAlpha,
        OneMinusDstAlpha,
    }
}
