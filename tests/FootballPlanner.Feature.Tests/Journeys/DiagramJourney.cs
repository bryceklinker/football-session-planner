using FootballPlanner.Feature.Tests.Infrastructure;
using Microsoft.Playwright;

namespace FootballPlanner.Feature.Tests.Journeys;

public class DiagramJourney(IPage page)
{
    public async Task OpenDiagramEditorAsync(string activityName)
    {
        await page.GotoAsync($"{FeatureTestFixture.BaseUrl}/activities");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.GetByText(activityName).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.GetByRole(AriaRole.Button, new() { Name = "Edit Diagram" }).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task SaveDiagramAsync()
    {
        await page.GetByTestId("save").ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task CancelDiagramAsync()
    {
        await page.GetByTestId("cancel").ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>Clicks a toolbar tool button by its aria-label (e.g. "Place cone").</summary>
    public async Task SelectToolAsync(string ariaLabel)
    {
        await page.GetByRole(AriaRole.Button, new() { Name = ariaLabel, Exact = true }).ClickAsync();
    }

    /// <summary>
    /// Clicks on the SVG canvas at a position specified as fractions of the canvas width/height
    /// (e.g. fractionX=0.5, fractionY=0.5 clicks the centre).
    /// </summary>
    public async Task ClickCanvasAsync(double fractionX, double fractionY)
    {
        var canvas = page.GetByTestId("diagram-canvas");
        var box = await canvas.BoundingBoxAsync()
            ?? throw new InvalidOperationException("Could not get canvas bounding box");
        await page.Mouse.ClickAsync(
            (float)(box.X + box.Width * fractionX),
            (float)(box.Y + box.Height * fractionY));
    }

    /// <summary>
    /// Drags the element identified by <paramref name="dataElement"/> to the canvas position
    /// specified as fractions of the canvas width/height.
    /// </summary>
    public async Task DragElementToAsync(string dataElement, double toFractionX, double toFractionY)
    {
        var canvas = page.GetByTestId("diagram-canvas");
        var canvasBox = await canvas.BoundingBoxAsync()
            ?? throw new InvalidOperationException("Could not get canvas bounding box");

        var el = page.Locator($"[data-element='{dataElement}']");
        var elBox = await el.BoundingBoxAsync()
            ?? throw new InvalidOperationException($"Could not get bounding box for element '{dataElement}'");

        var fromX = elBox.X + elBox.Width / 2;
        var fromY = elBox.Y + elBox.Height / 2;
        var toX = (float)(canvasBox.X + canvasBox.Width * toFractionX);
        var toY = (float)(canvasBox.Y + canvasBox.Height * toFractionY);

        await page.Mouse.MoveAsync(fromX, fromY);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(toX, toY, new() { Steps = 20 });
        await page.Mouse.UpAsync();
    }

    /// <summary>Clicks a specific diagram element (e.g. to delete it when the delete tool is active).</summary>
    public async Task ClickElementAsync(string dataElement)
    {
        await page.Locator($"[data-element='{dataElement}']").ClickAsync();
    }

    /// <summary>Returns the number of elements whose data-element starts with <paramref name="prefix"/>.</summary>
    public async Task<int> CountElementsAsync(string prefix)
    {
        return await page.Locator($"[data-element^='{prefix}']").CountAsync();
    }

    /// <summary>Takes a screenshot of the SVG canvas and returns the raw PNG bytes.</summary>
    public async Task<byte[]> TakeCanvasScreenshotAsync()
    {
        return await page.GetByTestId("diagram-canvas").ScreenshotAsync();
    }
}
