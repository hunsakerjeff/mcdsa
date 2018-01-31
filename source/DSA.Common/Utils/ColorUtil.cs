using System.Globalization;
using Windows.UI;

namespace DSA.Common.Utils
{
    public static class ColorUtil
    {
        public static Color FromString(string hexCode)
        {
            if(string.IsNullOrWhiteSpace(hexCode) || hexCode.Length < 6)
            {
                return new Color { A = 0, R = 0, G = 0, B = 0 };
            }

            return new Color
            {
                A = 255,
                R = byte.Parse(hexCode.Substring(0, 2), NumberStyles.AllowHexSpecifier),
                G = byte.Parse(hexCode.Substring(2, 2), NumberStyles.AllowHexSpecifier),
                B = byte.Parse(hexCode.Substring(4, 2), NumberStyles.AllowHexSpecifier),
            };
        }
    }
}