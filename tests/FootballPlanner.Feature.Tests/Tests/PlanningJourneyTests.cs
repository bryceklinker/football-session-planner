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
        await Assertions.Expect(fixture.Page.GetByText("Warm Up")).ToBeVisibleAsync();

        await fixture.Page.GotoAsync($"{FeatureTestFixture.BaseUrl}/focuses");
        await fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Assertions.Expect(fixture.Page.GetByText("Technique")).ToBeVisibleAsync();
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
        await Assertions.Expect(fixture.Page.GetByText("Rondo")).ToBeVisibleAsync();
        await Assertions.Expect(fixture.Page.GetByText("10 min")).ToBeVisibleAsync();
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
        await Assertions.Expect(fixture.Page.GetByText("Warm Up")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task CanOpenAndSaveDiagramEditor()
    {
        await fixture.NewPageAsync();

        await fixture.PhaseJourney.CreatePhaseAsync(new CreatePhaseInput("Warm Up", 1));
        await fixture.FocusJourney.CreateFocusAsync(new CreateFocusInput("Technique"));
        await fixture.ActivityJourney.CreateActivityAsync(
            new CreateActivityInput("Diagram Test Activity", "An activity with a diagram", 10));

        await fixture.DiagramJourney.OpenDiagramEditorAsync("Diagram Test Activity");

        // Modal should be open
        await Assertions.Expect(fixture.Page.GetByTestId("save")).ToBeVisibleAsync();

        // Save without placing anything
        await fixture.DiagramJourney.SaveDiagramAsync();

        // Modal should be closed, activity page shows "Diagram saved"
        await Assertions.Expect(fixture.Page.GetByText("Diagram saved")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task CanCancelDiagramEditor()
    {
        await fixture.NewPageAsync();

        await fixture.PhaseJourney.CreatePhaseAsync(new CreatePhaseInput("Warm Up", 1));
        await fixture.FocusJourney.CreateFocusAsync(new CreateFocusInput("Technique"));
        await fixture.ActivityJourney.CreateActivityAsync(
            new CreateActivityInput("Cancel Diagram Activity", "An activity for cancel test", 10));

        await fixture.DiagramJourney.OpenDiagramEditorAsync("Cancel Diagram Activity");

        // Modal should be open
        await Assertions.Expect(fixture.Page.GetByTestId("save")).ToBeVisibleAsync();

        // Cancel without saving
        await fixture.DiagramJourney.CancelDiagramAsync();

        // Modal should be closed — "No diagram yet" should still show
        await Assertions.Expect(fixture.Page.GetByText("No diagram yet")).ToBeVisibleAsync();
    }
}
