namespace Messentra.Features.Explorer.Resources;

public sealed record SearchQuery(
    string? NamePhrase,
    string? NamespaceFilter,
    bool HasDlq)
{
    public static readonly SearchQuery Empty = new(null, null, false);
    public bool IsEmpty => NamePhrase == null && NamespaceFilter == null && !HasDlq;
}

public static class SearchQueryParser
{
    private const string NamespacePrefix = "namespace:";
    private const string HasDlqToken = "has:dlq";

    public static SearchQuery Parse(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return SearchQuery.Empty;

        string? namespaceFilter = null;
        var hasDlq = false;
        var nameParts = new List<string>();

        foreach (var token in input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (token.StartsWith(NamespacePrefix, StringComparison.OrdinalIgnoreCase))
            {
                var value = token[NamespacePrefix.Length..];
                if (!string.IsNullOrEmpty(value))
                    namespaceFilter = value;
            }
            else if (token.Equals(HasDlqToken, StringComparison.OrdinalIgnoreCase))
                hasDlq = true;
            else
                nameParts.Add(token);
        }

        return new SearchQuery(
            NamePhrase: nameParts.Count > 0 ? string.Join(" ", nameParts) : null,
            NamespaceFilter: namespaceFilter,
            HasDlq: hasDlq);
    }
}

