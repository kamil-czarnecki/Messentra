using Bunit;
using Messentra.ComponentTests;
using Messentra.Features.Explorer.Messages.ActionProgress;
using MudBlazor;
using Shouldly;
using Xunit;

namespace Messentra.ComponentTests.Features.Explorer.Messages.ActionProgress;

public sealed class ActionProgressDialogShould : ComponentTestBase
{
    private static DialogParameters BuildParams(
        Func<IProgress<ActionProgressUpdate>, CancellationToken, Task>? onRunAction = null,
        int totalCount = 10)
    {
        var p = new DialogParameters
        {
            [nameof(ActionProgressDialog.ActionLabel)] = "Complete",
            [nameof(ActionProgressDialog.ActionIcon)] = "✓",
            [nameof(ActionProgressDialog.SubLabel)] = "orders-dlq",
            [nameof(ActionProgressDialog.TotalCount)] = totalCount,
            [nameof(ActionProgressDialog.OnRunAction)] = onRunAction
                                                         ?? ((_, _) => Task.CompletedTask)
        };
        return p;
    }

    [Fact]
    public void RendersActionLabelAndSubLabel()
    {
        // Arrange
        var cut = RenderDialog<ActionProgressDialog>(p =>
        {
            foreach (var (k, v) in BuildParams())
                p.Add(k, v);
        });

        // Assert
        cut.Markup.ShouldContain("Complete");
        cut.Markup.ShouldContain("orders-dlq");
    }

    [Fact]
    public void ShowsCancelButtonWhileRunning()
    {
        // Arrange
        var tcs = new TaskCompletionSource();
        var cut = RenderDialog<ActionProgressDialog>(p =>
        {
            foreach (var (k, v) in BuildParams(onRunAction: async (_, ct) => await tcs.Task))
                p.Add(k, v);
        });

        // Assert
        cut.FindAll("button").ShouldContain(b => b.TextContent.Contains("Cancel"));
        tcs.SetResult();
    }

    [Fact]
    public async Task ShowsInlineCancelConfirmationWhenCancelClicked()
    {
        // Arrange
        var tcs = new TaskCompletionSource();
        var cut = RenderDialog<ActionProgressDialog>(p =>
        {
            foreach (var (k, v) in BuildParams(onRunAction: async (_, ct) => await tcs.Task))
                p.Add(k, v);
        });

        // Act
        var cancelBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Cancel"));
        await cut.InvokeAsync(() => cancelBtn.Click());

        // Assert
        cut.Markup.ShouldContain("Cancel this action?");
        cut.FindAll("button").ShouldContain(b => b.TextContent.Contains("Keep going"));
        cut.FindAll("button").ShouldContain(b => b.TextContent.Contains("Yes, cancel"));
        tcs.SetResult();
    }

    [Fact]
    public async Task KeepGoingRestoresRunningState()
    {
        // Arrange
        var tcs = new TaskCompletionSource();
        var cut = RenderDialog<ActionProgressDialog>(p =>
        {
            foreach (var (k, v) in BuildParams(onRunAction: async (_, ct) => await tcs.Task))
                p.Add(k, v);
        });

        // Act
        var cancelBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Cancel"));
        await cut.InvokeAsync(() => cancelBtn.Click());

        var keepGoingBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Keep going"));
        await cut.InvokeAsync(() => keepGoingBtn.Click());

        // Assert
        cut.Markup.ShouldNotContain("Cancel this action?");
        cut.FindAll("button").ShouldContain(b => b.TextContent.Contains("Cancel"));
        tcs.SetResult();
    }

    [Fact]
    public async Task ShowsOkButtonWhenActionCompletes()
    {
        // Arrange
        var cut = RenderDialog<ActionProgressDialog>(p =>
        {
            foreach (var (k, v) in BuildParams(onRunAction: (_, _) => Task.CompletedTask))
                p.Add(k, v);
        });

        // Act
        await cut.InvokeAsync(() => { });

        // Assert
        cut.FindAll("button").ShouldContain(b => b.TextContent.Contains("OK"));
        cut.FindAll("button").ShouldNotContain(b => b.TextContent.Contains("Cancel"));
    }

    [Fact]
    public async Task ReportsSucceededAndFailedCounts()
    {
        // Arrange
        var cut = RenderDialog<ActionProgressDialog>(p =>
        {
            foreach (var (k, v) in BuildParams(
                totalCount: 3,
                onRunAction: (progress, _) =>
                {
                    progress.Report(new ActionProgressUpdate(2, 1, 0, "msg-001", "Timeout"));
                    return Task.CompletedTask;
                }))
                p.Add(k, v);
        });

        // Act
        await cut.InvokeAsync(() => { });

        // Assert
        cut.Markup.ShouldContain("msg-001");
        cut.Markup.ShouldContain("Timeout");
    }

    [Fact]
    public async Task YesCancelTransitionsToOkState()
    {
        // Arrange
        var cut = RenderDialog<ActionProgressDialog>(p =>
        {
            foreach (var (k, v) in BuildParams(onRunAction: async (_, ct) =>
                await Task.Delay(Timeout.Infinite, ct)))
                p.Add(k, v);
        });

        // Act
        var cancelBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Cancel"));
        await cut.InvokeAsync(() => cancelBtn.Click());

        var yesCancelBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Yes, cancel"));
        await cut.InvokeAsync(() => yesCancelBtn.Click());

        await cut.InvokeAsync(() => { });

        // Assert
        cut.FindAll("button").ShouldContain(b => b.TextContent.Contains("OK"));
        cut.FindAll("button").ShouldNotContain(b => b.TextContent.Contains("Cancel"));
    }
}
