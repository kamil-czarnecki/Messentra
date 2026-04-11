using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Messentra.Features.Explorer.Resources;

internal static class GlobMatcher
{
    private static readonly ConcurrentDictionary<string, Regex> RegexCache = new();

    public static bool Matches(string value, string pattern)
    {
        if (!pattern.Contains('*'))
            return string.Equals(value, pattern, StringComparison.OrdinalIgnoreCase);

        var regex = RegexCache.GetOrAdd(pattern, p =>
        {
            var escaped = Regex.Escape(p).Replace(@"\*", ".*");
            return new Regex(
                $"^{escaped}$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        });

        return regex.IsMatch(value);
    }
}