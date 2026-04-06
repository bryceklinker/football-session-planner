using FootballPlanner.Feature.Tests.Infrastructure;
using FootballPlanner.Feature.Tests.Journeys;
using Microsoft.Playwright;

namespace FootballPlanner.Feature.Tests.Tests;

public class PlanningJourneyTests(FeatureTestFixture fixture) : IClassFixture<FeatureTestFixture>
{
    [Fact]
    public async Task CanSetUpReferenceData()
    {
        await fixture.NewPageAsync();

        await fixture.PhaseJourney.CreatePhaseAsync(new CreatePhaseInput("Warm Up", 1));
        await fixture.FocusJourney.CreateFocusAsync(new CreateFocusInput("Technique"));

        await fixture.Page.GotoAsync($"{FeatureTestFixture.BaseUrl}/phases");
        await fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Assertions.Expect(fixture.Page.Locator("table")).ToContainTextAsync("Warm Up");

        await fixture.Page.GotoAsync($"{FeatureTestFixture.BaseUrl}/focuses");
        await fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Assertions.Expect(fixture.Page.Locator("table")).ToContainTextAsync("Technique");
    }

    [Fact]
    public async Task CanBuildActivityLibrary()
    {
        await fixture.NewPageAsync();

        await fixture.PhaseJourney.CreatePhaseAsync(new CreatePhaseInput("Warm Up", 1));
        await fixture.FocusJourney.CreateFocusAsync(new CreateFocusInput("Technique"));
        await fixture.ActivityJourney.CreateActivityAsync(new CreateActivityInput("Rondo", "A possession drill", 10));

        await fixture.Page.GotoAsync($"{FeatureTestFixture.BaseUrl}/activities");
        await fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Assertions.Expect(fixture.Page.Locator("table")).ToContainTextAsync("Rondo");
        await Assertions.Expect(fixture.Page.Locator("table")).ToContainTextAsync("A possession drill");
        await Assertions.Expect(fixture.Page.Locator("table")).ToContainTextAsync("10 min");
    }

    [Fact]
    public async Task CanPlanASession()
    {
        await fixture.NewPageAsync();

        await fixture.PhaseJourney.CreatePhaseAsync(new CreatePhaseInput("Warm Up", 1));
        await fixture.FocusJourney.CreateFocusAsync(new CreateFocusInput("Technique"));
        await fixture.ActivityJourney.CreateActivityAsync(new CreateActivityInput("Rondo", "A possession drill", 10));

        await fixture.SessionJourney.CreateSessionAsync(new CreateSessionInput("Tuesday Training", DateTime.Today));
        await fixture.SessionJourney.NavigateToEditorAsync("Tuesday Training");

        await fixture.SessionEditorJourney.AddActivityAsync(new AddActivityInput(
            ActivityName: "Rondo",
            ActivityEstimatedDuration: 10,
            PhaseName: "Warm Up",
            FocusName: "Technique",
            SessionDuration: 10));

        await Assertions.Expect(fixture.Page.GetByText("Rondo")).ToBeVisibleAsync();
        // Exact = false because the activity div renders "— Warm Up / Technique — 10 min"
        // and GetByText with exact matching would not find a partial substring
        await Assertions.Expect(fixture.Page.GetByText("Warm Up / Technique", new() { Exact = false })).ToBeVisibleAsync();
    }
}
