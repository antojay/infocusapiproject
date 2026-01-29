using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.Edi.Bone
{
    internal static class StringUtility
    {
        public static String EmptyStringIfNull(String str)
        {
            if(String.IsNullOrWhiteSpace(str))
            {
                return String.Empty;
            }
            return str;
        }
    }
}
