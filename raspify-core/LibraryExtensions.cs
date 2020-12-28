using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RaspifyCore
{
    static class LibraryExtensions
    {
        public static EndPoint Clone(this EndPoint source)
        {
            return IPEndPoint.Parse(source.ToString());
        }
    }
}
