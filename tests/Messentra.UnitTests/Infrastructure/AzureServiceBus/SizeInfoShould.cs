using Messentra.Infrastructure.AzureServiceBus;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Infrastructure.AzureServiceBus;

public sealed class SizeInfoShould
{
    [Fact]
    public void FreeSpacePercentage_WhenMaxSizeIsZero_ReturnsZero()
    {
        var sut = new SizeInfo(CurrentSizeInBytes: 0, MaxSizeInMegabytes: 0);
        sut.FreeSpacePercentage.ShouldBe(0);
    }

    [Fact]
    public void FreeSpacePercentage_WhenFullyFree_Returns100()
    {
        var sut = new SizeInfo(CurrentSizeInBytes: 0, MaxSizeInMegabytes: 1024);
        sut.FreeSpacePercentage.ShouldBe(100.0);
    }

    [Fact]
    public void FreeSpacePercentage_WhenHalfConsumed_Returns50()
    {
        const long maxMb = 1024;
        const long halfBytes = maxMb * 1024 * 1024 / 2;
        var sut = new SizeInfo(CurrentSizeInBytes: halfBytes, MaxSizeInMegabytes: maxMb);
        sut.FreeSpacePercentage.ShouldBe(50.0, tolerance: 0.001);
    }

    [Fact]
    public void FreeSpacePercentage_WhenFullyConsumed_ReturnsZero()
    {
        const long maxMb = 1024;
        const long fullBytes = maxMb * 1024 * 1024;
        var sut = new SizeInfo(CurrentSizeInBytes: fullBytes, MaxSizeInMegabytes: maxMb);
        sut.FreeSpacePercentage.ShouldBe(0.0);
    }
}

