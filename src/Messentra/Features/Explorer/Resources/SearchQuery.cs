using System.Text.RegularExpressions;

namespace Messentra.Features.Explorer.Resources;

public sealed record SearchQuery(
    string? NamePhrase,
    string? NamespaceFilter,
    bool HasDlq,
    string? FolderFilter)
{
    public static readonly SearchQuery Empty = new(null, null, false, null);
    public bool IsEmpty => NamePhrase == null && NamespaceFilter == null && !HasDlq && FolderFilter == null;
}

public static class SearchQueryParser
{
    private static readonly Regex NamespacePattern =
        new(@"(?i)\bnamespace:(?:""([^""]+)""|(\S*))", RegexOptions.Compiled);

    private static readonly Regex FoldersPattern =
        new(@"(?i)\bfolders:(?:""([^""]+)""|(\S*))", RegexOptions.Compiled);

    private const string HasDlqToken = "has:dlq";

    public static SearchQuery Parse(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return SearchQuery.Empty;

        var working = input.Trim();

        string? namespaceFilter = null;
        string? folderFilter = null;

        var nsMatch = NamespacePattern.Match(working);
        if (nsMatch.Success)
        {
            var value = nsMatch.Groups[1].Success ? nsMatch.Groups[1].Value : nsMatch.Groups[2].Value;
            if (!string.IsNullOrEmpty(value))
                namespaceFilter = value;
            working = working.Remove(nsMatch.Index, nsMatch.Length).Trim();
        }

        var foldersMatch = FoldersPattern.Match(working);
        if (foldersMatch.Success)
        {
            var value = foldersMatch.Groups[1].Success ? foldersMatch.Groups[1].Value : foldersMatch.Groups[2].Value;
            if (!string.IsNullOrEmpty(value))
                folderFilter = value;
            working = working.Remove(foldersMatch.Index, foldersMatch.Length).Trim();
        }

        var hasDlq = false;
        var nameParts = new List<string>();

        foreach (var token in working.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (token.Equals(HasDlqToken, StringComparison.OrdinalIgnoreCase))
                hasDlq = true;
            else
                nameParts.Add(token);
        }

        return new SearchQuery(
            NamePhrase: nameParts.Count > 0 ? string.Join(" ", nameParts) : null,
            NamespaceFilter: namespaceFilter,
            HasDlq: hasDlq,
            FolderFilter: folderFilter);
    }
}

