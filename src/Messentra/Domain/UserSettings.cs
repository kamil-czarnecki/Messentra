namespace Messentra.Domain;

public class UserSettings
{
    public long Id { get; init; }
    public bool IsDarkMode { get; set; }
    public bool IsMcpEnabled { get; set; }
    public int DefaultMessageCount { get; set; } = 100;
    public string? MessageGridViewsJson { get; set; }
    public string? ActiveMessageGridViewId { get; set; }
}
