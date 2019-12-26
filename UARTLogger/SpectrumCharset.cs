using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugins.UARTLogger
{
    public class SpectrumCharset
    {
        public static string ToASCII(byte Value)
        {
            if (Value < 32)
                return ".";
            return Encoding.ASCII.GetChars(new byte[] { Value })[0].ToString();
        }
    }
}
