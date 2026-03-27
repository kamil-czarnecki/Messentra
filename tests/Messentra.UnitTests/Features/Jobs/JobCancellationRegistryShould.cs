using AutoFixture;
using Messentra.Features.Jobs;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Jobs;

public sealed class JobCancellationRegistryShould
{
    private readonly Fixture _fixture = new();

    [Fact]
    public void ReturnActiveToken_WhenRegisterCalled()
    {
        // Arrange
        var sut = new JobCancellationRegistry();

        // Act
        var cts = sut.Register(_fixture.Create<long>());

        // Assert
        cts.IsCancellationRequested.ShouldBeFalse();
    }

    [Fact]
    public void CancelToken_WhenPauseRequestedForRegisteredJob()
    {
        // Arrange
        var sut = new JobCancellationRegistry();
        var jobId = _fixture.Create<long>();
        var cts = sut.Register(jobId);

        // Act
        sut.RequestPause(jobId);

        // Assert
        cts.IsCancellationRequested.ShouldBeTrue();
    }

    [Fact]
    public void NotThrow_WhenPauseRequestedForUnknownJob()
    {
        // Arrange
        var sut = new JobCancellationRegistry();

        // Act
        var action = () => sut.RequestPause(_fixture.Create<long>());

        // Assert
        action.ShouldNotThrow();
    }

    [Fact]
    public void DisposeToken_WhenUnregisterCalled()
    {
        // Arrange
        var sut = new JobCancellationRegistry();
        var jobId = _fixture.Create<long>();
        var cts = sut.Register(jobId);

        // Act
        sut.Unregister(jobId);

        // Assert
        Should.Throw<ObjectDisposedException>(() => cts.Cancel());
    }

    [Fact]
    public void NotThrow_WhenUnregisterCalledForUnknownJob()
    {
        // Arrange
        var sut = new JobCancellationRegistry();

        // Act
        var action = () => sut.Unregister(_fixture.Create<long>());

        // Assert
        action.ShouldNotThrow();
    }
}

