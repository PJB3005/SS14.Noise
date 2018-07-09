using OpenTK.Graphics;

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
    }
}