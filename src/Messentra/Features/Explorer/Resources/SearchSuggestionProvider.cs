using System.Text;
using MudBlazor;

namespace Messentra.Features.Explorer.Resources;

public sealed record SuggestionContext(
    IReadOnlyList<string> NamespaceNames,
    IReadOnlyList<string> FolderNames
);

public sealed record SearchTokenDescriptor(
    string Keyword,
    string Icon,
    Func<string, SuggestionContext, IEnumerable<string>> GetCompletions
);

public static class SearchSuggestionProvider
{
    private static IReadOnlyList<SearchTokenDescriptor> Tokens { get; } =
    [
        new(
            Keyword: "namespace:",
            Icon: Icons.Material.Filled.Cloud,
            GetCompletions: (partial, ctx) => ctx.NamespaceNames
                .Where(n => n.Contains(partial, StringComparison.OrdinalIgnoreCase))
                .Select(n => n.Contains(' ') ? $"namespace:\"{n}\"" : $"namespace:{n}")),

        new(
            Keyword: "folders:",
            Icon: Icons.Material.Filled.Folder,
            GetCompletions: (partial, ctx) =>
            {
                var suggestions = ctx.FolderNames
                    .Where(n => n.Contains(partial, StringComparison.OrdinalIgnoreCase))
                    .Select(n => n.Contains(' ') ? $"folders:\"{n}\"" : $"folders:{n}")
                    .ToList();

                if (string.IsNullOrEmpty(partial) || partial == "*")
                    suggestions.Insert(0, "folders:*");

                return suggestions;
            }),

        new(
            Keyword: "has:dlq",
            Icon: Icons.Material.Filled.AllInbox,
            GetCompletions: (_, _) => ["has:dlq"])
    ];

    public static IEnumerable<string> Suggest(string? input, SuggestionContext context)
    {
        var (tokens, lastTokenInOpenQuote) = Tokenize(input ?? "");
        var endsWithSpace = input?.EndsWith(' ') == true && !lastTokenInOpenQuote;

        string activeToken;
        string prefix;

        if (!endsWithSpace && tokens.Length > 0)
        {
            activeToken = tokens[^1];
            var completedTokens = tokens[..^1];
            prefix = completedTokens.Length > 0 ? string.Join(" ", completedTokens) + " " : "";
        }
        else
        {
            activeToken = "";
            prefix = tokens.Length > 0 ? string.Join(" ", tokens) + " " : "";
        }

        var descriptor = Tokens.FirstOrDefault(t =>
            activeToken.StartsWith(t.Keyword, StringComparison.OrdinalIgnoreCase));

        if (descriptor is not null)
        {
            var partial = activeToken[descriptor.Keyword.Length..].TrimStart('"');
            
            return descriptor.GetCompletions(partial, context)
                .Where(s => !string.Equals(prefix + s, input?.Trim(), StringComparison.OrdinalIgnoreCase))
                .Select(s => prefix + s)
                .Distinct()
                .Take(10);
        }

        return Tokens
            .Select(t => t.Keyword)
            .Where(k => k.StartsWith(activeToken, StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(k, activeToken, StringComparison.OrdinalIgnoreCase))
            .Select(k => prefix + k)
            .Take(10);
    }

    public static string GetIcon(string suggestion)
    {
        var (tokens, _) = Tokenize(suggestion.Trim());
        var lastToken = tokens.LastOrDefault() ?? suggestion;
        return Tokens
            .FirstOrDefault(t => lastToken.StartsWith(t.Keyword, StringComparison.OrdinalIgnoreCase))
            ?.Icon ?? Icons.Material.Filled.Search;
    }

    public static string GetLastToken(string suggestion)
    {
        var (tokens, _) = Tokenize(suggestion.Trim());
        return tokens.LastOrDefault() ?? suggestion;
    }

    private static (string[] Tokens, bool LastTokenInOpenQuote) Tokenize(string input)
    {
        var tokens = new List<string>();
        var current = new StringBuilder();
        var inQuote = false;

        for (var i = 0; i < input.Length; i++)
        {
            var ch = input[i];

            if (ch == '"' && i > 0 && input[i - 1] == ':')
                inQuote = true;
            else if (ch == '"' && inQuote)
                inQuote = false;

            if (ch == ' ' && !inQuote)
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(ch);
            }
        }

        var lastInOpenQuote = inQuote;

        if (current.Length > 0)
            tokens.Add(current.ToString());

        return (tokens.ToArray(), lastInOpenQuote);
    }
}
