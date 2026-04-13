using FootballPlanner.Feature.Tests.Infrastructure;
using Microsoft.Playwright;

namespace FootballPlanner.Feature.Tests.Journeys;

public record CreateActivityInput(string Name, string Description, int EstimatedDurationMinutes);

public class ActivityJourney(IPage page)
{
    public async Task CreateActivityAsync(CreateActivityInput input)
    {
        await page.GotoAsync($"{FeatureTestFixture.BaseUrl}/activities");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.GetByRole(AriaRole.Button, new() { Name = "New Activity" }).ClickAsync();
        await page.GetByLabel("Name").FillAsync(input.Name);
        await page.GetByLabel("Description").FillAsync(input.Description);
        await page.GetByLabel("Duration (min)").FillAsync(input.EstimatedDurationMinutes.ToString());
        await page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
