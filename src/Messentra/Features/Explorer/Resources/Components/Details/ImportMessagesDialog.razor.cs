using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Messentra.Features.Explorer.Resources.Components.Details;

public sealed record ImportMessagesDialogResult(IBrowserFile File);

public partial class ImportMessagesDialog
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    private IBrowserFile? _selectedFile;
    private static string TemplateJson => Template.Json;

    private void Cancel() => MudDialog.Cancel();

    private void Submit()
    {
        if (_selectedFile is null)
            return;

        MudDialog.Close(DialogResult.Ok(new ImportMessagesDialogResult(_selectedFile)));
    }
    
    private static class Template
    {
      public const string Json = """
                                 [
                                   {
                                     "message": {
                                       "id": "example-1",
                                       "type": "sample"
                                     },
                                     "properties": {
                                       "contentType": "application/json",
                                       "correlationId": "corr-001",
                                       "subject": "sample-subject",
                                       "messageId": "message-001",
                                       "to": null,
                                       "replyTo": null,
                                       "timeToLive": "00:05:00",
                                       "replyToSessionId": null,
                                       "sessionId": null,
                                       "partitionKey": null,
                                       "scheduledEnqueueTime": null,
                                       "transactionPartitionKey": null
                                     },
                                     "applicationProperties": {
                                       "source": "template",
                                       "priority": 1
                                     }
                                   },
                                   {
                                     "message": "text message example",
                                     "properties": {
                                       "contentType": "text/plain",
                                       "correlationId": "corr-002",
                                       "subject": "sample-subject-2",
                                       "messageId": "message-002",
                                       "to": null,
                                       "replyTo": null,
                                       "timeToLive": "00:05:00",
                                       "replyToSessionId": null,
                                       "sessionId": null,
                                       "partitionKey": null,
                                       "scheduledEnqueueTime": null,
                                       "transactionPartitionKey": null
                                     },
                                     "applicationProperties": {
                                       "source": "template",
                                       "priority": 1
                                     }
                                   }
                                 ]
                                 """;
    }
}

