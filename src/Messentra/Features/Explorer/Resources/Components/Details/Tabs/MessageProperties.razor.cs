using Messentra.Features.Explorer.Messages;
using Microsoft.AspNetCore.Components;

namespace Messentra.Features.Explorer.Resources.Components.Details.Tabs;

public partial class MessageProperties
{
    [Parameter] public required ServiceBusMessage Message { get; init; }
    [Parameter] public SubQueue SubQueue { get; init; }

    private BrokerProperties Props => Message.Message.BrokerProperties;

    private static string FormatDateTime(DateTime dt) =>
        dt.ToString("O");

    private static string FormatTimeSpan(TimeSpan ts) =>
        ts == TimeSpan.MaxValue ? "Max" : ts.ToString(@"d\.hh\:mm\:ss");
}

