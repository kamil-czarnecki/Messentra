using Mediator;

namespace Messentra.Features.Settings.Mcp.SaveMcpSettings;

public sealed record SaveMcpSettingsCommand(bool IsMcpEnabled) : ICommand;
