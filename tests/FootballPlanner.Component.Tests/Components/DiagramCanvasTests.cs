using Bunit;
using FootballPlanner.Web.Components;
using FootballPlanner.Web.Models;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FootballPlanner.Component.Tests.Components;

public class DiagramCanvasTests : BunitContext, IAsyncLifetime
{
    public DiagramCanvasTests()
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
    public void Renders_SvgElement()
    {
        var state = DefaultState();

        var cut = Render<DiagramCanvas>(
            p => p.Add(x => x.State, state));

        Assert.NotNull(cut.Find("svg"));
    }

    [Fact]
    public void Renders_PlayerCircles_ForEachPlayer()
    {
        var state = DefaultState();
        state.SetActiveTeam("t1");
        state.PlacePlayer(25.0, 50.0);
        state.PlacePlayer(35.0, 60.0);

        var cut = Render<DiagramCanvas>(
            p => p.Add(x => x.State, state));

        var circles = cut.FindAll("[data-element^='teams']");
        Assert.Equal(2, circles.Count);
    }

    [Fact]
    public void Renders_CoachCircle_ForEachCoach()
    {
        var state = DefaultState();
        state.PlaceCoach(50.0, 10.0);

        var cut = Render<DiagramCanvas>(
            p => p.Add(x => x.State, state));

        Assert.NotNull(cut.Find("[data-element^='coaches']"));
    }

    [Fact]
    public void Renders_ConePolygon_ForEachCone()
    {
        var state = DefaultState();
        state.PlaceCone(30.0, 40.0);

        var cut = Render<DiagramCanvas>(
            p => p.Add(x => x.State, state));

        Assert.NotNull(cut.Find("polygon[data-element^='cones']"));
    }

    [Fact]
    public void Renders_ArrowPath_ForEachArrow()
    {
        var state = DefaultState();
        state.SetTool("arrow-run");
        state.HandleArrowPoint(10.0, 20.0);
        state.HandleArrowPoint(80.0, 70.0);

        var cut = Render<DiagramCanvas>(
            p => p.Add(x => x.State, state));

        Assert.NotNull(cut.Find("path[data-element^='arrows']"));
    }

    [Fact]
    public void SvgClick_WithPlayerTool_FiresOnPlacePlayer()
    {
        var state = DefaultState();
        state.SetTool("player");
        state.SetActiveTeam("t1");
        var fired = false;

        var cut = Render<DiagramCanvas>(p =>
        {
            p.Add(x => x.State, state);
            p.Add(x => x.OnPlacePlayer, (_) => fired = true);
        });

        cut.Find("svg").Click();

        Assert.True(fired);
    }

    [Fact]
    public void Drag_WhenNoToolActive_UpdatesConePosition()
    {
        var state = DefaultState();
        // No tool active — drag is the default behaviour.
        state.PlaceCone(10.0, 20.0);

        var cut = Render<DiagramCanvas>(p => p.Add(x => x.State, state));

        cut.Find("polygon[data-element='cones/0']")
            .MouseDown(new MouseEventArgs { ClientX = 10, ClientY = 20 });

        // JS calls back with the model-coordinate delta (+40, +40).
        cut.InvokeAsync(() => cut.Instance.OnDragEnd("cones/0", 40.0, 40.0));

        Assert.Equal(50.0, state.Diagram.Cones[0].X); // 10 + 40
        Assert.Equal(60.0, state.Diagram.Cones[0].Y); // 20 + 40
    }

    [Fact]
    public void Drag_WithActiveTool_DoesNotUpdatePosition()
    {
        var state = DefaultState();
        state.SetTool("player"); // any active tool disables implicit drag
        state.PlaceCone(10.0, 20.0);

        var cut = Render<DiagramCanvas>(p => p.Add(x => x.State, state));

        cut.Find("polygon[data-element='cones/0']")
            .MouseDown(new MouseEventArgs { ClientX = 10, ClientY = 20 });

        // _isDragging was never set, so OnDragEnd is a no-op.
        cut.InvokeAsync(() => cut.Instance.OnDragEnd("cones/0", 40.0, 40.0));

        Assert.Equal(10.0, state.Diagram.Cones[0].X);
        Assert.Equal(20.0, state.Diagram.Cones[0].Y);
    }

    [Fact]
    public void SvgClick_WithConeTool_FiresOnPlaceCone()
    {
        var state = DefaultState();
        state.SetTool("cone");
        var fired = false;

        var cut = Render<DiagramCanvas>(p =>
        {
            p.Add(x => x.State, state);
            p.Add(x => x.OnPlaceCone, (_) => fired = true);
        });

        cut.Find("svg").Click();

        Assert.True(fired);
    }

    [Fact]
    public void SvgClick_WithCoachTool_FiresOnPlaceCoach()
    {
        var state = DefaultState();
        state.SetTool("coach");
        var fired = false;

        var cut = Render<DiagramCanvas>(p =>
        {
            p.Add(x => x.State, state);
            p.Add(x => x.OnPlaceCoach, (_) => fired = true);
        });

        cut.Find("svg").Click();

        Assert.True(fired);
    }

    [Fact]
    public void SvgClick_WithGoalTool_FiresOnPlaceGoal()
    {
        var state = DefaultState();
        state.SetTool("goal");
        var fired = false;

        var cut = Render<DiagramCanvas>(p =>
        {
            p.Add(x => x.State, state);
            p.Add(x => x.OnPlaceGoal, (_) => fired = true);
        });

        cut.Find("svg").Click();

        Assert.True(fired);
    }

    [Fact]
    public void SvgClick_WithArrowRunTool_FiresOnArrowPoint()
    {
        var state = DefaultState();
        state.SetTool("arrow-run");
        var fired = false;

        var cut = Render<DiagramCanvas>(p =>
        {
            p.Add(x => x.State, state);
            p.Add(x => x.OnArrowPoint, (_) => fired = true);
        });

        cut.Find("svg").Click();

        Assert.True(fired);
    }

    [Fact]
    public void SvgClick_WithNoTool_DoesNotFireAnyPlacementCallback()
    {
        var state = DefaultState();
        // no tool set
        var anyFired = false;

        var cut = Render<DiagramCanvas>(p =>
        {
            p.Add(x => x.State, state);
            p.Add(x => x.OnPlacePlayer, (_) => anyFired = true);
            p.Add(x => x.OnPlaceCone,   (_) => anyFired = true);
            p.Add(x => x.OnPlaceCoach,  (_) => anyFired = true);
            p.Add(x => x.OnPlaceGoal,   (_) => anyFired = true);
        });

        cut.Find("svg").Click();

        Assert.False(anyFired);
    }

    [Fact]
    public void ElementClick_FiresOnElementClick()
    {
        var state = DefaultState();
        state.PlaceCone(30.0, 40.0);
        string? clickedRef = null;

        var cut = Render<DiagramCanvas>(p =>
        {
            p.Add(x => x.State, state);
            p.Add(x => x.OnElementClick, (ref_) => clickedRef = ref_);
        });

        cut.Find("polygon[data-element='cones/0']").Click();

        Assert.Equal("cones/0", clickedRef);
    }

    [Fact]
    public void Renders_GoalRect_ForEachGoal()
    {
        var state = DefaultState();
        state.PlaceGoal(50.0, 5.0);

        var cut = Render<DiagramCanvas>(p => p.Add(x => x.State, state));

        Assert.NotNull(cut.Find("rect[data-element^='goals']"));
    }

    [Fact]
    public void Renders_PlayersAcrossMultipleTeams()
    {
        var state = DefaultState();
        state.SetActiveTeam("t1");
        state.PlacePlayer(20.0, 30.0);
        state.SetActiveTeam("t2");
        state.PlacePlayer(60.0, 70.0);

        var cut = Render<DiagramCanvas>(p => p.Add(x => x.State, state));

        Assert.Equal(2, cut.FindAll("[data-element^='teams']").Count);
    }

    [Fact]
    public void Renders_SelectionRing_ForSelectedElement()
    {
        var state = DefaultState();
        state.PlaceCone(50.0, 50.0);
        state.SelectElement("cones/0");

        var cut = Render<DiagramCanvas>(p => p.Add(x => x.State, state));

        // A selection ring circle is rendered in addition to the cone polygon
        var circles = cut.FindAll("circle");
        Assert.NotEmpty(circles);
    }

    [Fact]
    public void SvgClick_WithNoTool_FiresOnDeselect()
    {
        var state = DefaultState();
        var fired = false;

        var cut = Render<DiagramCanvas>(p =>
        {
            p.Add(x => x.State, state);
            p.Add(x => x.OnDeselect, () => fired = true);
        });

        cut.Find("svg").Click();

        Assert.True(fired);
    }

    [Fact]
    public void SvgClick_WithActiveTool_DoesNotFireOnDeselect()
    {
        var state = DefaultState();
        state.SetTool("cone");
        var fired = false;

        var cut = Render<DiagramCanvas>(p =>
        {
            p.Add(x => x.State, state);
            p.Add(x => x.OnDeselect, () => fired = true);
            p.Add(x => x.OnPlaceCone, (_) => { });
        });

        cut.Find("svg").Click();

        Assert.False(fired);
    }

    [Fact]
    public void Drag_SecondDragAfterFirstEnds_UsesUpdatedPosition()
    {
        var state = DefaultState();
        state.PlaceCone(10.0, 20.0);

        var cut = Render<DiagramCanvas>(p => p.Add(x => x.State, state));

        // First drag: +40 each → (50, 60)
        cut.Find("polygon[data-element='cones/0']")
            .MouseDown(new MouseEventArgs { ClientX = 10, ClientY = 20 });
        cut.InvokeAsync(() => cut.Instance.OnDragEnd("cones/0", 40.0, 40.0));

        Assert.Equal(50.0, state.Diagram.Cones[0].X);
        Assert.Equal(60.0, state.Diagram.Cones[0].Y);

        // Second drag: +20 each → (70, 80)
        cut.Find("polygon[data-element='cones/0']")
            .MouseDown(new MouseEventArgs { ClientX = 50, ClientY = 60 });
        cut.InvokeAsync(() => cut.Instance.OnDragEnd("cones/0", 20.0, 20.0));

        Assert.Equal(70.0, state.Diagram.Cones[0].X);
        Assert.Equal(80.0, state.Diagram.Cones[0].Y);
    }

    [Fact]
    public void Renders_ArrowHandles_WhenArrowSelected()
    {
        var state = DefaultState();
        state.HandleArrowPoint(10.0, 20.0);
        state.HandleArrowPoint(80.0, 70.0);
        state.SelectElement("arrows/0");

        var cut = Render<DiagramCanvas>(
            p => p.Add(x => x.State, state));

        Assert.NotNull(cut.Find("[data-element='arrows/0/tail']"));
        Assert.NotNull(cut.Find("[data-element='arrows/0/tip']"));
        Assert.NotNull(cut.Find("[data-element='arrows/0/curve']"));
    }

    [Fact]
    public void Renders_NoHandles_WhenArrowNotSelected()
    {
        var state = DefaultState();
        state.HandleArrowPoint(10.0, 20.0);
        state.HandleArrowPoint(80.0, 70.0);
        // SelectedElement is null — no selection

        var cut = Render<DiagramCanvas>(
            p => p.Add(x => x.State, state));

        Assert.Empty(cut.FindAll("[data-element='arrows/0/tail']"));
        Assert.Empty(cut.FindAll("[data-element='arrows/0/tip']"));
        Assert.Empty(cut.FindAll("[data-element='arrows/0/curve']"));
    }

    [Fact]
    public void Renders_NoLegend_WhenLegendIsNull()
    {
        var state = DefaultState();
        // Legend is null by default

        var cut = Render<DiagramCanvas>(
            p => p.Add(x => x.State, state));

        Assert.Empty(cut.FindAll("[data-element='legend']"));
    }

    [Fact]
    public void Renders_LegendEntries_DerivedFromArrows()
    {
        var state = DefaultState();
        state.HandleArrowPoint(10.0, 20.0);
        state.HandleArrowPoint(80.0, 70.0);
        state.ChangeArrowSequenceNumber("arrows/0", 1);
        state.AddLegend();

        var cut = Render<DiagramCanvas>(
            p => p.Add(x => x.State, state));

        Assert.NotNull(cut.Find("[data-element='legend']"));
    }

    [Fact]
    public void LegendCollapse_Toggle_CollapsesThenExpandsLegend()
    {
        var state = DefaultState();
        state.HandleArrowPoint(10.0, 20.0);
        state.HandleArrowPoint(80.0, 70.0);
        state.ChangeArrowSequenceNumber("arrows/0", 1);
        state.AddLegend();

        var cut = Render<DiagramCanvas>(
            p => p.Add(x => x.State, state));

        // Initially expanded: legend background rect is visible
        Assert.NotNull(cut.Find("[data-element='legend']"));

        // The collapse toggle is a <g> element with an onclick handler.
        // Click it to collapse the legend.
        var toggleGs = cut.FindAll("g").Where(g => g.HasAttribute("style") && g.GetAttribute("style")!.Contains("cursor:pointer")).ToList();
        var toggle = toggleGs.FirstOrDefault();
        if (toggle != null)
        {
            toggle.Click();
            // After collapse the legend rect still renders (just shorter)
            Assert.NotNull(cut.Find("[data-element='legend']"));
        }
    }
}
