using Mediator;

namespace Messentra.Features.Settings.FetchOptions.SaveFetchOptions;

public sealed record SaveFetchOptionsCommand(int DefaultMessageCount) : ICommand;
