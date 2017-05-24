using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitterBuild
{
    static class Extensions
    {
        public static string OrIfBlank(this string self, string @default)
            => string.IsNullOrWhiteSpace(self) ? @default : self;

        public static Exception InnermostException(this Exception e)
        {
            for (; e.InnerException != null; e = e.InnerException);
            return e;
        }
    }
}
