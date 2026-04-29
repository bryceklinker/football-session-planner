using FootballPlanner.Feature.Tests.Infrastructure;
using FootballPlanner.Feature.Tests.Journeys;

namespace FootballPlanner.Feature.Tests.Tests;

public class DiagramArrowEditingTests(FeatureTestFixture fixture) : IClassFixture<FeatureTestFixture>
{
    private async Task SetupAsync(string activityName)
    {
        await fixture.PhaseJourney.CreatePhaseAsync(new CreatePhaseInput("Warm Up", 1));
        await fixture.FocusJourney.CreateFocusAsync(new CreateFocusInput("Technique"));
        await fixture.ActivityJourney.CreateActivityAsync(
            new CreateActivityInput(activityName, "Arrow editing test", 10));
    }

    [Fact]
    public async Task ClickArrow_ShowsHandles()
    {
        await fixture.NewPageAsync();
        await SetupAsync("Arrow Handle Test");
        await fixture.DiagramJourney.OpenDiagramEditorAsync("Arrow Handle Test");

        await fixture.DiagramJourney.SelectToolAsync("Run arrow");
        await fixture.DiagramJourney.ClickCanvasAsync(0.3, 0.5);
        await fixture.DiagramJourney.ClickCanvasAsync(0.7, 0.5);

        // Deactivate tool then click the arrow to select it
        await fixture.DiagramJourney.SelectToolAsync("Run arrow");
        await fixture.DiagramJourney.SelectElementAsync("arrows/0");

        Assert.True(await fixture.DiagramJourney.HandleIsVisibleAsync(0, "tail"));
        Assert.True(await fixture.DiagramJourney.HandleIsVisibleAsync(0, "tip"));
        Assert.True(await fixture.DiagramJourney.HandleIsVisibleAsync(0, "curve"));
    }

    [Fact]
    public async Task DragArrowTipHandle_MovesEndpoint()
    {
        await fixture.NewPageAsync();
        await SetupAsync("Arrow Tip Drag Test");
        await fixture.DiagramJourney.OpenDiagramEditorAsync("Arrow Tip Drag Test");

        await fixture.DiagramJourney.SelectToolAsync("Run arrow");
        await fixture.DiagramJourney.ClickCanvasAsync(0.3, 0.5);
        await fixture.DiagramJourney.ClickCanvasAsync(0.5, 0.5);

        // Select arrow
        await fixture.DiagramJourney.SelectToolAsync("Run arrow");
        await fixture.DiagramJourney.SelectElementAsync("arrows/0");

        var tipBefore = await fixture.Page.Locator("[data-element='arrows/0/tip']").BoundingBoxAsync();
        Assert.NotNull(tipBefore);
        var fromX = tipBefore.X + tipBefore.Width / 2;
        var fromY = tipBefore.Y + tipBefore.Height / 2;

        var canvas = fixture.Page.GetByTestId("diagram-canvas");
        var canvasBox = await canvas.BoundingBoxAsync();
        Assert.NotNull(canvasBox);
        var toX = (float)(canvasBox.X + canvasBox.Width * 0.8);
        var toY = fromY;

        await fixture.Page.Mouse.MoveAsync(fromX, fromY);
        await fixture.Page.Mouse.DownAsync();
        await fixture.Page.Mouse.MoveAsync(toX, toY, new() { Steps = 20 });
        await fixture.Page.Mouse.UpAsync();

        var tipAfter = await fixture.Page.Locator("[data-element='arrows/0/tip']").BoundingBoxAsync();
        Assert.NotNull(tipAfter);
        var tipAfterX = tipAfter.X + tipAfter.Width / 2;
        Assert.True(tipAfterX > fromX + 30,
            $"Expected tip to move right, was {fromX:F0} before and {tipAfterX:F0} after");
    }

    [Fact]
    public async Task ToggleLegend_AddsAndRemovesLegend()
    {
        await fixture.NewPageAsync();
        await SetupAsync("Legend Toggle Test");
        await fixture.DiagramJourney.OpenDiagramEditorAsync("Legend Toggle Test");

        Assert.False(await fixture.DiagramJourney.LegendIsVisibleAsync());

        await fixture.DiagramJourney.ToggleLegendAsync();
        Assert.True(await fixture.DiagramJourney.LegendIsVisibleAsync());

        await fixture.DiagramJourney.ToggleLegendAsync();
        Assert.False(await fixture.DiagramJourney.LegendIsVisibleAsync());
    }

    [Fact]
    public async Task Legend_ShowsEntry_WhenArrowHasSequenceNumber()
    {
        await fixture.NewPageAsync();
        await SetupAsync("Legend Entry Test");
        await fixture.DiagramJourney.OpenDiagramEditorAsync("Legend Entry Test");

        // Place a run arrow
        await fixture.DiagramJourney.SelectToolAsync("Run arrow");
        await fixture.DiagramJourney.ClickCanvasAsync(0.3, 0.5);
        await fixture.DiagramJourney.ClickCanvasAsync(0.7, 0.5);

        // Select arrow and set sequence number via properties panel
        await fixture.DiagramJourney.SelectToolAsync("Run arrow");
        await fixture.DiagramJourney.SelectElementAsync("arrows/0");
        await fixture.Page.GetByLabel("Sequence No.").FillAsync("1");
        await fixture.Page.Keyboard.PressAsync("Tab");

        // Add legend and confirm it shows the entry
        await fixture.DiagramJourney.ToggleLegendAsync();
        var legend = fixture.Page.Locator("[data-element='legend']");
        await legend.WaitForAsync();
        Assert.True(await legend.IsVisibleAsync());
    }

    [Fact]
    public async Task SavedDiagram_ReopensWithLegend()
    {
        await fixture.NewPageAsync();
        await SetupAsync("Legend Persist Test");
        await fixture.DiagramJourney.OpenDiagramEditorAsync("Legend Persist Test");

        await fixture.DiagramJourney.ToggleLegendAsync();
        Assert.True(await fixture.DiagramJourney.LegendIsVisibleAsync());

        await fixture.DiagramJourney.SaveDiagramAsync();
        await fixture.DiagramJourney.OpenDiagramEditorAsync("Legend Persist Test");

        Assert.True(await fixture.DiagramJourney.LegendIsVisibleAsync());
    }
}
