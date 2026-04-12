using System.Text.RegularExpressions;

namespace Messentra.Features.Explorer.Resources;

internal static class GlobMatcher
{
    public static bool Matches(string value, string pattern)
    {
        if (!pattern.Contains('*'))
            return string.Equals(value, pattern, StringComparison.OrdinalIgnoreCase);
        
        var escaped = Regex.Escape(pattern).Replace(@"\*", ".*");
        var regex = new Regex(
            $"^{escaped}$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        return regex.IsMatch(value);
    }
}