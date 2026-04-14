using Bunit;
using FootballPlanner.Web.Components;
using FootballPlanner.Web.Models;
using MudBlazor.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FootballPlanner.Component.Tests.Components;

public class DiagramTeamsPanelTests : TestContext
{
    public DiagramTeamsPanelTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static DiagramEditorState DefaultState()
    {
        var state = new DiagramEditorState();
        state.Initialize(null);
        return state;
    }

    [Fact]
    public void RendersDefaultTeams()
    {
        var state = DefaultState();

        var cut = RenderComponent<DiagramTeamsPanel>(
            p => p.Add(x => x.State, state));

        Assert.Contains("Red", cut.Markup);
        Assert.Contains("Blue", cut.Markup);
    }

    [Fact]
    public void ClickAddTeam_AddsThirdTeam()
    {
        var state = DefaultState();
        var cut = RenderComponent<DiagramTeamsPanel>(
            p => p.Add(x => x.State, state));

        cut.Find("[data-testid='add-team']").Click();

        Assert.Equal(3, state.Diagram.Teams.Count);
    }

    [Fact]
    public void ClickDeleteTeam_RemovesTeam()
    {
        var state = DefaultState();
        var cut = RenderComponent<DiagramTeamsPanel>(
            p => p.Add(x => x.State, state));

        cut.Find("[data-testid='delete-team-t1']").Click();

        Assert.Single(state.Diagram.Teams);
        Assert.DoesNotContain(state.Diagram.Teams, t => t.Id == "t1");
    }

    [Fact]
    public void ClickTeam_SetsActiveTeamId()
    {
        var state = DefaultState();
        var cut = RenderComponent<DiagramTeamsPanel>(
            p => p.Add(x => x.State, state));

        cut.Find("[data-testid='select-team-t2']").Click();

        Assert.Equal("t2", state.ActiveTeamId);
    }
}
