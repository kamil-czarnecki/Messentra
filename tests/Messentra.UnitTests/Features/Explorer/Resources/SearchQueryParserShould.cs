using Messentra.Features.Explorer.Resources;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Explorer.Resources;

public sealed class SearchQueryParserShould
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ReturnEmptyQueryForBlankInput(string? input)
    {
        SearchQueryParser.Parse(input).ShouldBe(SearchQuery.Empty);
    }

    [Fact]
    public void ParsePlainTextAsNamePhrase()
    {
        var query = SearchQueryParser.Parse("queue1");
        query.NamePhrase.ShouldBe("queue1");
        query.NamespaceFilter.ShouldBeNull();
        query.HasDlq.ShouldBeFalse();
    }

    [Fact]
    public void CombineMultiplePlainWordsIntoNamePhrase()
    {
        var query = SearchQueryParser.Parse("order processing");
        query.NamePhrase.ShouldBe("order processing");
    }

    [Fact]
    public void ParseNamespacePrefix()
    {
        var query = SearchQueryParser.Parse("namespace:prod");
        query.NamespaceFilter.ShouldBe("prod");
        query.NamePhrase.ShouldBeNull();
        query.HasDlq.ShouldBeFalse();
    }

    [Fact]
    public void ParseNamespacePrefixCaseInsensitive()
    {
        SearchQueryParser.Parse("NAMESPACE:prod").NamespaceFilter.ShouldBe("prod");
        SearchQueryParser.Parse("Namespace:prod").NamespaceFilter.ShouldBe("prod");
    }

    [Fact]
    public void IgnoreEmptyNamespaceValue()
    {
        var query = SearchQueryParser.Parse("namespace:");
        query.NamespaceFilter.ShouldBeNull();
        query.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void ParseHasDlqToken()
    {
        var query = SearchQueryParser.Parse("has:dlq");
        query.HasDlq.ShouldBeTrue();
        query.NamePhrase.ShouldBeNull();
        query.NamespaceFilter.ShouldBeNull();
    }

    [Fact]
    public void ParseHasDlqTokenCaseInsensitive()
    {
        SearchQueryParser.Parse("HAS:DLQ").HasDlq.ShouldBeTrue();
        SearchQueryParser.Parse("Has:Dlq").HasDlq.ShouldBeTrue();
    }

    [Fact]
    public void ParseCombinedNamespaceAndHasDlq()
    {
        var query = SearchQueryParser.Parse("namespace:prod has:dlq");
        query.NamespaceFilter.ShouldBe("prod");
        query.HasDlq.ShouldBeTrue();
        query.NamePhrase.ShouldBeNull();
    }

    [Fact]
    public void ParseCombinedNamePhraseAndHasDlq()
    {
        var query = SearchQueryParser.Parse("queue1 has:dlq");
        query.NamePhrase.ShouldBe("queue1");
        query.HasDlq.ShouldBeTrue();
        query.NamespaceFilter.ShouldBeNull();
    }

    [Fact]
    public void ParseAllThreeTokenTypes()
    {
        var query = SearchQueryParser.Parse("namespace:prod queue1 has:dlq");
        query.NamespaceFilter.ShouldBe("prod");
        query.NamePhrase.ShouldBe("queue1");
        query.HasDlq.ShouldBeTrue();
    }

    [Fact]
    public void IsEmptyReturnsFalseWhenAnyConditionIsSet()
    {
        SearchQueryParser.Parse("queue1").IsEmpty.ShouldBeFalse();
        SearchQueryParser.Parse("namespace:prod").IsEmpty.ShouldBeFalse();
        SearchQueryParser.Parse("has:dlq").IsEmpty.ShouldBeFalse();
    }
}

