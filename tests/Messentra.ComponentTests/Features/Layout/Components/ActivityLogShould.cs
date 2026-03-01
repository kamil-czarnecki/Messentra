using AutoFixture;
using Bunit;
using Messentra.Features.Layout.Components;
using Messentra.Features.Layout.State;
using MudBlazor;
using MudBlazor.Extensions;
using Shouldly;
using Xunit;

namespace Messentra.ComponentTests.Features.Layout.Components;

public sealed class ActivityLogShould : ComponentTestBase
{
    [Fact]
    public void RenderDrawerWithCorrectInitialState()
    {
        // Arrange & Act
        var cut = Render<ActivityLog>();

        // Assert
        var drawer = cut.FindComponent<MudDrawer>();
        drawer.Instance.GetState(x => x.Open).ShouldBeTrue();
        drawer.Instance.Anchor.ShouldBe(Anchor.Bottom);
        drawer.Instance.Variant.ShouldBe(DrawerVariant.Persistent);
    }

    [Fact]
    public void RenderLogCountFromState()
    {
        // Arrange
        var state = GetState<ActivityLogState>();
        var logs = Fixture.CreateMany<ActivityLogEntry>(3).ToArray();
        state.SetState(new ActivityLogState(logs));

        // Act
        var cut = Render<ActivityLog>();

        // Assert
        cut.Markup.ShouldContain("3");
    }

    [Fact]
    public void NotDisplayLogsWhenDrawerIsClosed()
    {
        // Arrange
        var state = GetState<ActivityLogState>();
        var logs = new[]
        {
            new ActivityLogEntry("Connection 1", "Info", "Test message", DateTime.Now)
        };
        state.SetState(new ActivityLogState(logs));

        // Act
        var cut = Render<ActivityLog>();

        cut.FindAll(".mud-alert").Count.ShouldBe(0);
    }

    [Fact]
    public void DisplayLogsWhenDrawerIsOpened()
    {
        // Arrange
        var state = GetState<ActivityLogState>();
        var logs = new[]
        {
            new ActivityLogEntry("Connection 1", "Info", "Test message", DateTime.Now)
        };
        state.SetState(new ActivityLogState(logs));
        var cut = Render<ActivityLog>();

        // Act - Click toggle to open drawer
        var toggleIcon = cut.Find(".cursor-pointer");
        toggleIcon.Click();

        // Assert
        cut.FindAll(".mud-alert").Count.ShouldBe(1);
        cut.Markup.ShouldContain("Test message");
        cut.Markup.ShouldContain("Connection 1");
    }

    [Fact]
    public void DisplayAllLogsWhenDrawerIsOpened()
    {
        // Arrange
        var state = GetState<ActivityLogState>();
        var logs = new[]
        {
            new ActivityLogEntry("Connection 1", "Info", "Info message 1", DateTime.Now),
            new ActivityLogEntry("Connection 1", "Error", "Error message 1", DateTime.Now),
            new ActivityLogEntry("Connection 2", "Info", "Info message 2", DateTime.Now),
            new ActivityLogEntry("Connection 2", "Warning", "Warning message 2", DateTime.Now)
        };
        state.SetState(new ActivityLogState(logs));
        var cut = Render<ActivityLog>();
        
        // Act - Open drawer
        cut.Find(".cursor-pointer").Click();

        // Assert - All logs visible
        cut.FindAll(".mud-alert").Count.ShouldBe(4);
        cut.Markup.ShouldContain("Info message 1");
        cut.Markup.ShouldContain("Error message 1");
        cut.Markup.ShouldContain("Info message 2");
        cut.Markup.ShouldContain("Warning message 2");
    }

    [Fact]
    public void RenderLogsWithCorrectSeverity()
    {
        // Arrange
        var state = GetState<ActivityLogState>();
        var logs = new[]
        {
            new ActivityLogEntry("Connection 1", "Info", "Info message", DateTime.Now.AddMinutes(-2)),
            new ActivityLogEntry("Connection 1", "Warning", "Warning message", DateTime.Now.AddMinutes(-1)),
            new ActivityLogEntry("Connection 1", "Error", "Error message", DateTime.Now)
        };
        state.SetState(new ActivityLogState(logs));
        var cut = Render<ActivityLog>();
        
        // Open drawer
        cut.Find(".cursor-pointer").Click();

        // Act
        var alerts = cut.FindComponents<MudAlert>();

        // Assert - Logs are ordered by timestamp descending (most recent first)
        alerts[0].Instance.Severity.ShouldBe(Severity.Error);
        alerts[1].Instance.Severity.ShouldBe(Severity.Warning);
        alerts[2].Instance.Severity.ShouldBe(Severity.Info);
    }

    [Fact]
    public void RenderFilterDropdowns()
    {
        // Arrange & Act
        var cut = Render<ActivityLog>();

        // Assert
        var selects = cut.FindComponents<MudSelect<string>>();
        selects.Count.ShouldBe(2); // Connection and Log Level dropdowns
    }
}

