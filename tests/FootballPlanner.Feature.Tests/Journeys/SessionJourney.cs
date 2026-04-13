using FootballPlanner.Feature.Tests.Infrastructure;
using Microsoft.Playwright;

namespace FootballPlanner.Feature.Tests.Journeys;

public record CreateSessionInput(string Title, DateTime Date);

public class SessionJourney(IPage page)
{
    public async Task CreateSessionAsync(CreateSessionInput input)
    {
        await page.GotoAsync($"{FeatureTestFixture.BaseUrl}/sessions");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.GetByRole(AriaRole.Button, new() { Name = "New Session" }).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.GetByLabel("Title").FillAsync(input.Title);
        await page.GetByLabel("Date").FillAsync(input.Date.ToString("MM/dd/yyyy"));
        await page.GetByRole(AriaRole.Button, new() { Name = "Create" }).ClickAsync();

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task NavigateToEditorAsync(string title)
    {
        await page.GotoAsync($"{FeatureTestFixture.BaseUrl}/sessions");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var card = page.Locator(".mud-card").Filter(new() { HasText = title });
        await card.GetByRole(AriaRole.Button, new() { Name = "Open" }).ClickAsync();

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
