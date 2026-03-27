using AutoFixture;
using Messentra.Features.Jobs;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Jobs;

public sealed class TypedJobShould
{
    private readonly Fixture _fixture = new();

    [Fact]
    public void RoundTripInput_WhenInitializedWithValue()
    {
        // Arrange
        var expected = new TestInput(_fixture.Create<string>(), _fixture.Create<int>());
        var sut = new TestTypedJob
        {
            Label = _fixture.Create<string>(),
            CreatedAt = DateTime.UtcNow,
            Input = expected
        };

        // Act
        var input = sut.Input;

        // Assert
        input.ShouldBe(expected);
    }

    [Fact]
    public void ReturnNullInput_WhenInitializedWithNull()
    {
        // Arrange
        var sut = new TestTypedJob
        {
            Label = _fixture.Create<string>(),
            CreatedAt = DateTime.UtcNow,
            Input = null
        };

        // Act
        var input = sut.Input;

        // Assert
        input.ShouldBeNull();
    }

    [Fact]
    public void RoundTripOutput_WhenSetByDerivedType()
    {
        // Arrange
        var expected = new TestOutput(_fixture.Create<string>());
        var sut = new TestTypedJob
        {
            Label = _fixture.Create<string>(),
            CreatedAt = DateTime.UtcNow,
            Input = new TestInput(_fixture.Create<string>(), _fixture.Create<int>())
        };

        // Act
        sut.SetOutput(expected);

        // Assert
        sut.Output.ShouldBe(expected);
    }

    [Fact]
    public void ReturnNullOutput_WhenNotSet()
    {
        // Arrange
        var sut = new TestTypedJob
        {
            Label = _fixture.Create<string>(),
            CreatedAt = DateTime.UtcNow,
            Input = new TestInput(_fixture.Create<string>(), _fixture.Create<int>())
        };

        // Act
        var output = sut.Output;

        // Assert
        output.ShouldBeNull();
    }

    private sealed class TestTypedJob : TypedJob<TestInput, TestOutput>
    {
        public override IReadOnlyList<Type> Stages { get; } = [];

        public void SetOutput(TestOutput output)
        {
            Output = output;
        }
    }

    private sealed record TestInput(string Name, int Count);

    private sealed record TestOutput(string Path);
}

