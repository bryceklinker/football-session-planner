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

        await page.Locator("input[type='date']").FillAsync(input.Date.ToString("yyyy-MM-dd"));
        await page.GetByPlaceholder("Title").FillAsync(input.Title);
        await page.GetByRole(AriaRole.Button, new() { Name = "Add Session" }).ClickAsync();

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task NavigateToEditorAsync(string title)
    {
        await page.GotoAsync($"{FeatureTestFixture.BaseUrl}/sessions");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var row = page.Locator("tr").Filter(new() { HasText = title });
        await row.GetByRole(AriaRole.Button, new() { Name = "Edit" }).ClickAsync();

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
