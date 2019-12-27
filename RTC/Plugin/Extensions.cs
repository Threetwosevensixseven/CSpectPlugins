using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugins.RTC.Plugin
{
    public static class Extensions
    {
        public static string ToLogString(this IEnumerable<byte> This)
        {
            if (This == null)
                return "";
            return string.Join("", This.Select(b => b.ToString("x2")));
        }

        public static string ToASCII(this IEnumerable<byte> This)
        {
            if (This == null || This.Count() == 0)
                return "";
            return " (" + Encoding.ASCII.GetString(This.ToArray()) + ")";
        }

        public static byte FromBCD(this byte This)
        {
            byte left = Convert.ToByte((This & 0xf0) >> 4);
            byte right = Convert.ToByte(This & 0x0f);
            if (left < 0 || left > 9 || right < 0 || right > 9)
                return 0;
            return Convert.ToByte((left * 10) + right);
        }

        public static byte FromBCH(this byte This)
        {
            byte left = Convert.ToByte((This & 0xf0) >> 4);
            byte right = Convert.ToByte(This & 0x0f);
            if (left < 0 || left > 15 || right < 0 || right > 15)
                return 0;
            return Convert.ToByte((left * 10) + right);
        }

    }
}
