using System.Linq;
using System.Text.RegularExpressions;

namespace YamlGenerator
{
    public static class Utilities
    {
        public static string ReplaceAll(this string seed, char[] chars, char replacementCharacter)
        {
            return chars.Aggregate(seed, (str, cItem) => str.Replace(cItem, replacementCharacter));
        }

        public static string TruncateCommas(string input)
        {
            return Regex.Replace(input, @",+", ",");
        }

        public static string FormatElementName(string input)
        {
            string result = input.Substring(input.IndexOf(".") + 1).Trim();
            result = result.Substring(result.IndexOf(".") + 1).Trim();
            result = result.Replace(".", "/");
            return result;
        }
    }
}
