namespace Messentra.Domain;

public class UserSettings
{
    public long Id { get; init; }
    public bool IsDarkMode { get; set; }
    public string? MessageGridViewsJson { get; set; }
    public string? ActiveMessageGridViewId { get; set; }
}
