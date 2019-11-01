using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TestNuGetExtensions")]

namespace Matthid.WiX.NuGetExtensions
{
    public static class Extensions
    {
        public static bool ContainsEx(this string text, string value,
            StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
        {
            return text.IndexOf(value, stringComparison) >= 0;
        }
    }
}