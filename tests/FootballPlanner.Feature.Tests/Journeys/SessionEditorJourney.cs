using Microsoft.Playwright;

namespace FootballPlanner.Feature.Tests.Journeys;

/// <param name="ActivityName">Name of the activity to select from the picker dialog.</param>
/// <param name="ActivityEstimatedDuration">Not used in the new picker-based flow, kept for backwards compatibility.</param>
/// <param name="PhaseName">Not used in the new picker-based flow; defaults are assigned automatically.</param>
/// <param name="FocusName">Not used in the new picker-based flow; defaults are assigned automatically.</param>
/// <param name="SessionDuration">Not used in the new picker-based flow; activity's estimated duration is used.</param>
public record AddActivityInput(
    string ActivityName,
    int ActivityEstimatedDuration,
    string PhaseName,
    string FocusName,
    int SessionDuration);

public class SessionEditorJourney(IPage page)
{
    public async Task AddActivityAsync(AddActivityInput input)
    {
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.GetByRole(AriaRole.Button, new() { Name = "Add Activity" }).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Click the activity row in the dialog's list
        await page.GetByRole(AriaRole.Dialog)
            .Locator(".mud-list-item")
            .Filter(new() { HasText = input.ActivityName })
            .ClickAsync();

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
