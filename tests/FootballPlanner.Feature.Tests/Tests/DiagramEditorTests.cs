using FootballPlanner.Feature.Tests.Infrastructure;
using FootballPlanner.Feature.Tests.Journeys;

namespace FootballPlanner.Feature.Tests.Tests;

public class DiagramEditorTests(FeatureTestFixture fixture) : IClassFixture<FeatureTestFixture>
{
    // ── helpers ──────────────────────────────────────────────────────────────

    private async Task SetupPrerequisitesAsync(string activityName)
    {
        await fixture.PhaseJourney.CreatePhaseAsync(new CreatePhaseInput("Warm Up", 1));
        await fixture.FocusJourney.CreateFocusAsync(new CreateFocusInput("Technique"));
        await fixture.ActivityJourney.CreateActivityAsync(
            new CreateActivityInput(activityName, "Diagram editor test activity", 10));
    }

    // ── placement ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CanPlaceCones()
    {
        await fixture.NewPageAsync();
        await SetupPrerequisitesAsync("Cone Placement Test");

        await fixture.DiagramJourney.OpenDiagramEditorAsync("Cone Placement Test");

        await fixture.DiagramJourney.SelectToolAsync("Place cone");
        await fixture.DiagramJourney.ClickCanvasAsync(0.3, 0.4);
        await fixture.DiagramJourney.ClickCanvasAsync(0.5, 0.5);
        await fixture.DiagramJourney.ClickCanvasAsync(0.7, 0.4);

        Assert.Equal(3, await fixture.DiagramJourney.CountElementsAsync("cones"));
    }

    [Fact]
    public async Task CanPlacePlayers()
    {
        await fixture.NewPageAsync();
        await SetupPrerequisitesAsync("Player Placement Test");

        await fixture.DiagramJourney.OpenDiagramEditorAsync("Player Placement Test");

        await fixture.DiagramJourney.SelectToolAsync("Place player");
        await fixture.DiagramJourney.ClickCanvasAsync(0.4, 0.5);
        await fixture.DiagramJourney.ClickCanvasAsync(0.6, 0.5);

        Assert.Equal(2, await fixture.DiagramJourney.CountElementsAsync("teams"));
    }

    [Fact]
    public async Task CanPlaceArrow()
    {
        await fixture.NewPageAsync();
        await SetupPrerequisitesAsync("Arrow Placement Test");

        await fixture.DiagramJourney.OpenDiagramEditorAsync("Arrow Placement Test");

        // Arrow requires two clicks: start point then end point
        await fixture.DiagramJourney.SelectToolAsync("Run arrow");
        await fixture.DiagramJourney.ClickCanvasAsync(0.3, 0.5);
        await fixture.DiagramJourney.ClickCanvasAsync(0.7, 0.5);

        Assert.Equal(1, await fixture.DiagramJourney.CountElementsAsync("arrows"));
    }

    // ── delete ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task CanDeleteElement()
    {
        await fixture.NewPageAsync();
        await SetupPrerequisitesAsync("Delete Element Test");

        await fixture.DiagramJourney.OpenDiagramEditorAsync("Delete Element Test");

        await fixture.DiagramJourney.SelectToolAsync("Place cone");
        await fixture.DiagramJourney.ClickCanvasAsync(0.5, 0.5);
        Assert.Equal(1, await fixture.DiagramJourney.CountElementsAsync("cones"));

        await fixture.DiagramJourney.SelectToolAsync("Delete element");
        await fixture.DiagramJourney.ClickElementAsync("cones/0");

        Assert.Equal(0, await fixture.DiagramJourney.CountElementsAsync("cones"));
    }

    // ── drag ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CanDragConeToNewPosition()
    {
        await fixture.NewPageAsync();
        await SetupPrerequisitesAsync("Drag Test");

        await fixture.DiagramJourney.OpenDiagramEditorAsync("Drag Test");

        // Place a cone near the left side
        await fixture.DiagramJourney.SelectToolAsync("Place cone");
        await fixture.DiagramJourney.ClickCanvasAsync(0.2, 0.5);

        // Deselect the tool so drag is active
        await fixture.DiagramJourney.SelectToolAsync("Place cone");

        var canvas = fixture.Page.GetByTestId("diagram-canvas");
        var canvasBoxBefore = await canvas.BoundingBoxAsync();
        var coneBefore = await fixture.Page.Locator("[data-element='cones/0']").BoundingBoxAsync();

        Assert.NotNull(canvasBoxBefore);
        Assert.NotNull(coneBefore);

        var coneXBefore = coneBefore.X + coneBefore.Width / 2 - canvasBoxBefore.X;

        // Drag the cone to the right side of the canvas
        await fixture.DiagramJourney.DragElementToAsync("cones/0", 0.8, 0.5);

        var canvasBoxAfter = await canvas.BoundingBoxAsync();
        var coneAfter = await fixture.Page.Locator("[data-element='cones/0']").BoundingBoxAsync();

        Assert.NotNull(canvasBoxAfter);
        Assert.NotNull(coneAfter);

        var coneXAfter = coneAfter.X + coneAfter.Width / 2 - canvasBoxAfter.X;

        // Cone should have moved significantly to the right
        Assert.True(coneXAfter > coneXBefore + 50,
            $"Expected cone to move right but was at {coneXBefore:.0f}px before and {coneXAfter:.0f}px after");
    }

    // ── persist ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task SavedDiagram_ReopenedInEditor_MatchesScreenshotBeforeSave()
    {
        await fixture.NewPageAsync();
        await SetupPrerequisitesAsync("Screenshot Persist Test");

        await fixture.DiagramJourney.OpenDiagramEditorAsync("Screenshot Persist Test");

        // Place a cone, player, and arrow
        await fixture.DiagramJourney.SelectToolAsync("Place cone");
        await fixture.DiagramJourney.ClickCanvasAsync(0.3, 0.4);
        await fixture.DiagramJourney.ClickCanvasAsync(0.7, 0.6);

        await fixture.DiagramJourney.SelectToolAsync("Place player");
        await fixture.DiagramJourney.ClickCanvasAsync(0.5, 0.5);

        await fixture.DiagramJourney.SelectToolAsync("Run arrow");
        await fixture.DiagramJourney.ClickCanvasAsync(0.3, 0.4);
        await fixture.DiagramJourney.ClickCanvasAsync(0.5, 0.5);

        // Screenshot before save (deselect tool first so no ghost cursor)
        await fixture.DiagramJourney.SelectToolAsync("Run arrow");
        var screenshotBefore = await fixture.DiagramJourney.TakeCanvasScreenshotAsync();

        // Save then reopen
        await fixture.DiagramJourney.SaveDiagramAsync();
        await fixture.DiagramJourney.OpenDiagramEditorAsync("Screenshot Persist Test");

        var screenshotAfter = await fixture.DiagramJourney.TakeCanvasScreenshotAsync();

        Assert.Equal(screenshotBefore, screenshotAfter);
    }

    [Fact]
    public async Task SavedDiagram_ReopenedInEditor_HasSameElementCounts()
    {
        await fixture.NewPageAsync();
        await SetupPrerequisitesAsync("Element Count Persist Test");

        await fixture.DiagramJourney.OpenDiagramEditorAsync("Element Count Persist Test");

        await fixture.DiagramJourney.SelectToolAsync("Place cone");
        await fixture.DiagramJourney.ClickCanvasAsync(0.3, 0.4);
        await fixture.DiagramJourney.ClickCanvasAsync(0.6, 0.6);

        await fixture.DiagramJourney.SelectToolAsync("Place player");
        await fixture.DiagramJourney.ClickCanvasAsync(0.5, 0.5);

        await fixture.DiagramJourney.SelectToolAsync("Run arrow");
        await fixture.DiagramJourney.ClickCanvasAsync(0.2, 0.3);
        await fixture.DiagramJourney.ClickCanvasAsync(0.8, 0.7);

        // Verify counts before saving
        Assert.Equal(2, await fixture.DiagramJourney.CountElementsAsync("cones"));
        Assert.Equal(1, await fixture.DiagramJourney.CountElementsAsync("teams"));
        Assert.Equal(1, await fixture.DiagramJourney.CountElementsAsync("arrows"));

        await fixture.DiagramJourney.SaveDiagramAsync();

        // Reopen and verify the same counts
        await fixture.DiagramJourney.OpenDiagramEditorAsync("Element Count Persist Test");

        Assert.Equal(2, await fixture.DiagramJourney.CountElementsAsync("cones"));
        Assert.Equal(1, await fixture.DiagramJourney.CountElementsAsync("teams"));
        Assert.Equal(1, await fixture.DiagramJourney.CountElementsAsync("arrows"));
    }
}
