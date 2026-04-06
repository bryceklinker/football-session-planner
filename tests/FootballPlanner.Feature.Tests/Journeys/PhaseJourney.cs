using FootballPlanner.Feature.Tests.Infrastructure;
using Microsoft.Playwright;

namespace FootballPlanner.Feature.Tests.Journeys;

public record CreatePhaseInput(string Name, int Order);

public class PhaseJourney(IPage page)
{
    public async Task CreatePhaseAsync(CreatePhaseInput input)
    {
        await page.GotoAsync($"{FeatureTestFixture.BaseUrl}/phases");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.GetByPlaceholder("Phase name").FillAsync(input.Name);
        await page.GetByPlaceholder("Order").FillAsync(input.Order.ToString());
        await page.GetByRole(AriaRole.Button, new() { Name = "Add" }).ClickAsync();

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
