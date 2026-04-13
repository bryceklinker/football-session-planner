using FootballPlanner.Feature.Tests.Infrastructure;
using Microsoft.Playwright;

namespace FootballPlanner.Feature.Tests.Journeys;

public record CreateFocusInput(string Name);

public class FocusJourney(IPage page)
{
    public async Task CreateFocusAsync(CreateFocusInput input)
    {
        await page.GotoAsync($"{FeatureTestFixture.BaseUrl}/focuses");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.GetByRole(AriaRole.Button, new() { Name = "New Focus" }).ClickAsync();
        await page.GetByLabel("Name").FillAsync(input.Name);
        await page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
