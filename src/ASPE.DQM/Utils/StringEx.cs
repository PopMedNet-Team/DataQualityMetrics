using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASPE.DQM.Utils
{
    public static class StringEx
    {
        public static string ToStringEx(this object value)
        {
            if (value == null)
                return string.Empty;

            return ToStringEx(value.ToString());
        }

        public static string ToStringEx(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value;
        }

        public static string Ellipse(this string value, int maxLength, string append)
        {
            var s = value.ToStringEx();
            if (s.Length <= (maxLength - append.Length))
                return s;

            return s.Substring(0, maxLength - append.Length) + append;
        }
    }
}
