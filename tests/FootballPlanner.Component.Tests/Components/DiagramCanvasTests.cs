using Bunit;
using FootballPlanner.Web.Components;
using FootballPlanner.Web.Models;
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
    public async Task Drag_WhenMouseReleased_UpdatesConePosition()
    {
        var state = DefaultState();
        state.SetTool("move");
        state.PlaceCone(10.0, 20.0);

        JSInterop.Mode = JSRuntimeMode.Strict;
        JSInterop.SetupVoid("diagramInterop.attachDrag", _ => true);
        JSInterop.SetupVoid("diagramInterop.cleanup", _ => true);

        var cut = Render<DiagramCanvas>(p => p.Add(x => x.State, state));

        // JS calls OnElementMouseDown when mousedown fires on [data-element]
        cut.Instance.OnElementMouseDown("cones/0");
        // JS calls OnDragEnd with final position on mouseup
        await cut.Instance.OnDragEnd(50.0, 60.0);

        Assert.Equal(50.0, state.Diagram.Cones[0].X);
        Assert.Equal(60.0, state.Diagram.Cones[0].Y);
    }

    [Fact]
    public async Task Drag_WithNonMoveTool_DoesNotUpdatePosition()
    {
        var state = DefaultState();
        state.SetTool("player"); // not "move"
        state.PlaceCone(10.0, 20.0);

        var cut = Render<DiagramCanvas>(p => p.Add(x => x.State, state));

        // OnElementMouseDown returns early when tool != "move", _draggingRef stays null
        cut.Instance.OnElementMouseDown("cones/0");
        // OnDragEnd: _draggingRef is null — PreviewMove not called
        await cut.Instance.OnDragEnd(50.0, 60.0);

        Assert.Equal(10.0, state.Diagram.Cones[0].X);
        Assert.Equal(20.0, state.Diagram.Cones[0].Y);
    }

    [Fact]
    public async Task Drag_SecondDragAfterFirstEnds_DoesNotUpdatePosition()
    {
        var state = DefaultState();
        state.SetTool("move");
        state.PlaceCone(10.0, 20.0);

        JSInterop.Mode = JSRuntimeMode.Strict;
        JSInterop.SetupVoid("diagramInterop.attachDrag", _ => true);
        JSInterop.SetupVoid("diagramInterop.cleanup", _ => true);

        var cut = Render<DiagramCanvas>(p => p.Add(x => x.State, state));

        cut.Instance.OnElementMouseDown("cones/0");
        await cut.Instance.OnDragEnd(50.0, 60.0);

        // After drag end, _draggingRef is null — a second OnDragEnd should be no-op
        await cut.Instance.OnDragEnd(90.0, 90.0);

        Assert.Equal(50.0, state.Diagram.Cones[0].X);
        Assert.Equal(60.0, state.Diagram.Cones[0].Y);
    }
}
