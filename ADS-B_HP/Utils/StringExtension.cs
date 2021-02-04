using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADS_B_HP.Utils
{
    public static class StringExtension
    {
        public static bool IsNullorEmpty(this string source)
        {
            return string.IsNullOrEmpty(source);
        }

        public static bool IsNotNullorEmpty(this string source)
        {
            return !string.IsNullOrEmpty(source);
        }
    }
}
