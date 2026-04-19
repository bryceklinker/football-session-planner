using Bunit;
using FootballPlanner.Web.Components;
using FootballPlanner.Web.Models;
using MudBlazor.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FootballPlanner.Component.Tests.Components;

public class DiagramTeamsPanelTests : BunitContext, IAsyncLifetime
{
    public DiagramTeamsPanelTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await ((IAsyncDisposable)this).DisposeAsync();
    protected override void Dispose(bool disposing) { }

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

        var cut = Render<DiagramTeamsPanel>(
            p => p.Add(x => x.State, state));

        Assert.Contains("Red", cut.Markup);
        Assert.Contains("Blue", cut.Markup);
    }

    [Fact]
    public void ClickAddTeam_AddsThirdTeam()
    {
        var state = DefaultState();
        var cut = Render<DiagramTeamsPanel>(
            p => p.Add(x => x.State, state));

        cut.Find("[data-testid='add-team']").Click();

        Assert.Equal(3, state.Diagram.Teams.Count);
    }

    [Fact]
    public void ClickDeleteTeam_RemovesTeam()
    {
        var state = DefaultState();
        var cut = Render<DiagramTeamsPanel>(
            p => p.Add(x => x.State, state));

        cut.Find("[aria-label='Delete team t1']").Click();

        Assert.Single(state.Diagram.Teams);
        Assert.DoesNotContain(state.Diagram.Teams, t => t.Id == "t1");
    }

    [Fact]
    public void ClickTeam_SetsActiveTeamId()
    {
        var state = DefaultState();
        var cut = Render<DiagramTeamsPanel>(
            p => p.Add(x => x.State, state));

        cut.Find("[data-testid='select-team-t2']").Click();

        Assert.Equal("t2", state.ActiveTeamId);
    }
}
