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

        await page.GetByPlaceholder("Name").FillAsync(input.Name);
        await page.GetByPlaceholder("Description").FillAsync(input.Description);
        await page.GetByPlaceholder("Duration (mins)").FillAsync(input.EstimatedDurationMinutes.ToString());
        await page.GetByRole(AriaRole.Button, new() { Name = "Add" }).ClickAsync();

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
