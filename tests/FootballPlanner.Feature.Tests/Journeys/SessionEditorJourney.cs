using Microsoft.Playwright;

namespace FootballPlanner.Feature.Tests.Journeys;

/// <param name="ActivityName">Name of the activity to select from the dropdown.</param>
/// <param name="ActivityEstimatedDuration">Estimated duration used when the activity was created — needed to match the dropdown label "{name} ({duration} min)".</param>
/// <param name="PhaseName">Phase to assign to this session activity.</param>
/// <param name="FocusName">Focus to assign to this session activity.</param>
/// <param name="SessionDuration">How long this activity will run in the session (may differ from estimated duration).</param>
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

        // Activity dropdown option text is "{name} ({estimatedDuration} min)"
        await page.Locator("select")
            .Filter(new() { HasText = "-- Select Activity --" })
            .SelectOptionAsync(new SelectOptionValue { Label = $"{input.ActivityName} ({input.ActivityEstimatedDuration} min)" });

        await page.Locator("select")
            .Filter(new() { HasText = "-- Select Phase --" })
            .SelectOptionAsync(new SelectOptionValue { Label = input.PhaseName });

        await page.Locator("select")
            .Filter(new() { HasText = "-- Select Focus --" })
            .SelectOptionAsync(new SelectOptionValue { Label = input.FocusName });

        await page.GetByPlaceholder("Duration (mins)").FillAsync(input.SessionDuration.ToString());
        await page.GetByRole(AriaRole.Button, new() { Name = "Add Activity" }).ClickAsync();

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
