using Messentra.Features.Explorer.Resources;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Explorer.Resources;

public sealed class SearchSuggestionProviderShould
{
    private static SuggestionContext EmptyContext() =>
        new(NamespaceNames: [], FolderNames: []);

    private static SuggestionContext WithNamespaces(params string[] names) =>
        new(NamespaceNames: names, FolderNames: []);

    private static SuggestionContext WithFolders(params string[] names) =>
        new(NamespaceNames: [], FolderNames: names);

    [Fact]
    public void EmptyInput_SuggestsAllBaseKeywords()
    {
        var result = SearchSuggestionProvider.Suggest("", EmptyContext()).ToList();
        result.ShouldContain("namespace:");
        result.ShouldContain("folders:");
        result.ShouldContain("has:dlq");
    }

    [Fact]
    public void NullInput_SuggestsAllBaseKeywords()
    {
        var result = SearchSuggestionProvider.Suggest(null, EmptyContext()).ToList();
        result.ShouldContain("namespace:");
        result.ShouldContain("folders:");
        result.ShouldContain("has:dlq");
    }

    [Fact]
    public void PartialKeyword_SuggestsMatchingKeyword()
    {
        var result = SearchSuggestionProvider.Suggest("n", EmptyContext()).ToList();
        result.ShouldContain("namespace:");
        result.ShouldNotContain("folders:");
        result.ShouldNotContain("has:dlq");
    }

    [Fact]
    public void HasPrefix_SuggestsHasDlq()
    {
        var result = SearchSuggestionProvider.Suggest("has:", EmptyContext()).ToList();
        result.ShouldContain("has:dlq");
    }

    [Fact]
    public void NamespaceColon_SuggestsAllNamespaces()
    {
        var result = SearchSuggestionProvider.Suggest("namespace:", WithNamespaces("prod", "dev")).ToList();
        result.ShouldContain("namespace:prod");
        result.ShouldContain("namespace:dev");
    }

    [Fact]
    public void NamespacePartial_SuggestsMatchingNamespaces()
    {
        var result = SearchSuggestionProvider.Suggest("namespace:pr", WithNamespaces("prod", "dev")).ToList();
        result.ShouldContain("namespace:prod");
        result.ShouldNotContain("namespace:dev");
    }

    [Fact]
    public void NamespaceWithSpaces_IsQuotedInSuggestion()
    {
        var result = SearchSuggestionProvider.Suggest("namespace:", WithNamespaces("my namespace")).ToList();
        result.ShouldContain("namespace:\"my namespace\"");
    }

    [Fact]
    public void NamespaceWithoutSpaces_IsNotQuoted()
    {
        var result = SearchSuggestionProvider.Suggest("namespace:", WithNamespaces("prod")).ToList();
        result.ShouldContain("namespace:prod");
        result.ShouldNotContain("namespace:\"prod\"");
    }

    [Fact]
    public void FoldersColon_SuggestsStarAndAllFolders()
    {
        var result = SearchSuggestionProvider.Suggest("folders:", WithFolders("team", "prod")).ToList();
        result.ShouldContain("folders:*");
        result.ShouldContain("folders:team");
        result.ShouldContain("folders:prod");
    }

    [Fact]
    public void FoldersPartial_SuggestsMatchingFolders()
    {
        var result = SearchSuggestionProvider.Suggest("folders:te", WithFolders("team", "prod")).ToList();
        result.ShouldContain("folders:team");
        result.ShouldNotContain("folders:prod");
    }

    [Fact]
    public void FoldersWithSpaces_IsQuotedInSuggestion()
    {
        var result = SearchSuggestionProvider.Suggest("folders:", WithFolders("my team")).ToList();
        result.ShouldContain("folders:\"my team\"");
    }

    [Fact]
    public void FoldersStar_SuggestsStarWhenTypingPartialStar()
    {
        var result = SearchSuggestionProvider.Suggest("folders:", WithFolders()).ToList();
        result.ShouldContain("folders:*");
    }

    [Fact]
    public void FoldersStar_DoesNotResuggestStarWhenInputIsAlreadyFoldersStar()
    {
        var result = SearchSuggestionProvider.Suggest("folders:*", WithFolders("team")).ToList();
        result.ShouldNotContain("folders:*");
    }

    [Fact]
    public void QuotedFolderNameWithSpaces_SuggestsCorrectlyWhileTyping()
    {
        var result = SearchSuggestionProvider.Suggest(
            "folders:\"my f",
            WithFolders("my folder", "my files", "other")).ToList();

        result.ShouldContain("folders:\"my folder\"");
        result.ShouldContain("folders:\"my files\"");
        result.ShouldNotContain("folders:other");
    }

    [Fact]
    public void QuotedNamespaceWithSpaces_SuggestsCorrectlyWhileTyping()
    {
        var result = SearchSuggestionProvider.Suggest(
            "namespace:\"west ",
            WithNamespaces("west prod", "west dev", "east")).ToList();

        result.ShouldContain("namespace:\"west prod\"");
        result.ShouldContain("namespace:\"west dev\"");
        result.ShouldNotContain("namespace:east");
    }

    [Fact]
    public void WithCompletedPrefix_PreservesItInSuggestions()
    {
        var result = SearchSuggestionProvider.Suggest(
            "has:dlq namespace:pr",
            WithNamespaces("prod", "dev")).ToList();

        result.ShouldContain("has:dlq namespace:prod");
        result.ShouldNotContain("has:dlq namespace:dev");
    }

    [Fact]
    public void ReturnsAtMostTenSuggestions()
    {
        var manyFolders = Enumerable.Range(1, 20).Select(i => $"folder{i}").ToArray();
        var result = SearchSuggestionProvider.Suggest("folders:", WithFolders(manyFolders)).ToList();
        result.Count.ShouldBeLessThanOrEqualTo(10);
    }

    [Fact]
    public void DoesNotSuggestCurrentExactInput()
    {
        var result = SearchSuggestionProvider.Suggest("namespace:prod", WithNamespaces("prod")).ToList();
        result.ShouldNotContain("namespace:prod");
    }

    [Fact]
    public void GetIcon_ReturnsCorrectIconForKnownKeyword()
    {
        SearchSuggestionProvider.GetIcon("namespace:prod").ShouldBe(MudBlazor.Icons.Material.Filled.Cloud);
        SearchSuggestionProvider.GetIcon("folders:team").ShouldBe(MudBlazor.Icons.Material.Filled.Folder);
        SearchSuggestionProvider.GetIcon("has:dlq").ShouldBe(MudBlazor.Icons.Material.Filled.AllInbox);
    }
}
