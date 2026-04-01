namespace Messentra.Infrastructure.AutoUpdater;

public sealed class AutoUpdatePollingOptions
{
	public const string SectionName = "AutoUpdate";

	public int CheckIntervalMinutes { get; init; } = 30;

	public TimeSpan CheckInterval => TimeSpan.FromMinutes(Math.Max(1, CheckIntervalMinutes));
}

