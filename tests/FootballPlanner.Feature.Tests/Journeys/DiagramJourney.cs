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
        await page.GetByRole(AriaRole.Button, new() { Name = "Cancel" }).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
