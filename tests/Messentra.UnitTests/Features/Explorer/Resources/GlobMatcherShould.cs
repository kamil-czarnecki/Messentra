using Messentra.Features.Explorer.Resources;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Explorer.Resources;

public sealed class GlobMatcherShould
{
    [Fact]
    public void ExactMatch_ReturnsTrueForIdenticalName()
    {
        GlobMatcher.Matches("prod", "prod").ShouldBeTrue();
    }

    [Fact]
    public void ExactMatch_ReturnsTrueForSameNameCaseInsensitive()
    {
        GlobMatcher.Matches("Prod", "prod").ShouldBeTrue();
        GlobMatcher.Matches("prod", "PROD").ShouldBeTrue();
    }

    [Fact]
    public void ExactMatch_ReturnsFalseForPartialMatch()
    {
        GlobMatcher.Matches("prod-west", "prod").ShouldBeFalse();
    }

    [Fact]
    public void ExactMatch_ReturnsFalseForSubstringMatch()
    {
        GlobMatcher.Matches("west-prod", "prod").ShouldBeFalse();
    }

    [Fact]
    public void ExactMatch_ReturnsTrueForNameWithSpaces()
    {
        GlobMatcher.Matches("my team", "my team").ShouldBeTrue();
    }

    [Fact]
    public void ExactMatch_ReturnsFalseForDifferentName()
    {
        GlobMatcher.Matches("dev", "prod").ShouldBeFalse();
    }

    [Fact]
    public void GlobMatch_StarMatchesAnySuffix()
    {
        GlobMatcher.Matches("prod-west", "prod*").ShouldBeTrue();
        GlobMatcher.Matches("prod-east", "prod*").ShouldBeTrue();
    }

    [Fact]
    public void GlobMatch_StarMatchesEmptySuffix()
    {
        GlobMatcher.Matches("prod", "prod*").ShouldBeTrue();
    }

    [Fact]
    public void GlobMatch_StarAloneMatchesAnything()
    {
        GlobMatcher.Matches("anything", "*").ShouldBeTrue();
        GlobMatcher.Matches("prod-west", "*").ShouldBeTrue();
    }

    [Fact]
    public void GlobMatch_StarMatchesAnyPrefix()
    {
        GlobMatcher.Matches("west-prod", "*prod").ShouldBeTrue();
    }

    [Fact]
    public void GlobMatch_StarInMiddle()
    {
        GlobMatcher.Matches("prod-west-1", "prod*1").ShouldBeTrue();
        GlobMatcher.Matches("prod-east-1", "prod*1").ShouldBeTrue();
    }

    [Fact]
    public void GlobMatch_ReturnsFalseWhenPatternDoesNotMatch()
    {
        GlobMatcher.Matches("dev-west", "prod*").ShouldBeFalse();
    }

    [Fact]
    public void GlobMatch_IsCaseInsensitive()
    {
        GlobMatcher.Matches("PROD-WEST", "prod*").ShouldBeTrue();
    }

    [Fact]
    public void GlobMatch_MultipleStars()
    {
        GlobMatcher.Matches("prod-team-west", "prod*west").ShouldBeTrue();
        GlobMatcher.Matches("prod-team-east", "prod*west").ShouldBeFalse();
    }

    [Fact]
    public void GlobMatch_RegexSpecialCharsInPatternAreEscaped()
    {
        GlobMatcher.Matches("prod.west", "prod.west").ShouldBeTrue();
        GlobMatcher.Matches("prodXwest", "prod.west").ShouldBeFalse();
    }

    [Fact]
    public void GlobMatch_StarAloneMatchesEmptyString()
    {
        GlobMatcher.Matches("", "*").ShouldBeTrue();
    }
}