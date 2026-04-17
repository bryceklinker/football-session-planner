using Bunit;
using FootballPlanner.Web.Components;
using FootballPlanner.Web.Models;
using MudBlazor.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FootballPlanner.Component.Tests.Components;

public class DiagramCanvasTests : TestContext
{
    public DiagramCanvasTests()
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
    public void Renders_SvgElement()
    {
        var state = DefaultState();

        var cut = RenderComponent<DiagramCanvas>(
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

        var cut = RenderComponent<DiagramCanvas>(
            p => p.Add(x => x.State, state));

        var circles = cut.FindAll("circle[data-element]");
        Assert.Equal(2, circles.Count);
    }

    [Fact]
    public void Renders_CoachCircle_ForEachCoach()
    {
        var state = DefaultState();
        state.PlaceCoach(50.0, 10.0);

        var cut = RenderComponent<DiagramCanvas>(
            p => p.Add(x => x.State, state));

        Assert.NotNull(cut.Find("circle[data-element^='coaches']"));
    }

    [Fact]
    public void Renders_ConePolygon_ForEachCone()
    {
        var state = DefaultState();
        state.PlaceCone(30.0, 40.0);

        var cut = RenderComponent<DiagramCanvas>(
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

        var cut = RenderComponent<DiagramCanvas>(
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

        var cut = RenderComponent<DiagramCanvas>(p =>
        {
            p.Add(x => x.State, state);
            p.Add(x => x.OnPlacePlayer, (_) => fired = true);
        });

        cut.Find("svg").Click();

        Assert.True(fired);
    }

    [Fact]
    public void OnDragMove_UpdatesElementPosition()
    {
        var state = DefaultState();
        state.SetTool("move");
        state.PlaceCone(10.0, 20.0);

        var cut = RenderComponent<DiagramCanvas>(
            p => p.Add(x => x.State, state));

        // Mousedown on the cone element starts the drag (sets _draggingRef)
        cut.Find("polygon[data-element^='cones']").MouseDown();

        // Simulate JS callback
        cut.Instance.OnDragMove(50.0, 60.0);
        cut.Render();

        Assert.Equal(50.0, state.Diagram.Cones[0].X);
        Assert.Equal(60.0, state.Diagram.Cones[0].Y);
    }

    [Fact]
    public void OnMouseDown_WithNonMoveTool_DoesNotStartDrag()
    {
        var state = DefaultState();
        state.SetTool("player"); // not "move"
        state.PlaceCone(10.0, 20.0);

        var cut = RenderComponent<DiagramCanvas>(
            p => p.Add(x => x.State, state));

        cut.Find("polygon[data-element^='cones']").MouseDown();

        // OnDragMove should be a no-op since _draggingRef was never set
        cut.Instance.OnDragMove(50.0, 60.0);
        cut.Render();

        // Position should be unchanged
        Assert.Equal(10.0, state.Diagram.Cones[0].X);
        Assert.Equal(20.0, state.Diagram.Cones[0].Y);
    }

    [Fact]
    public void OnDragComplete_UpdatesPositionAndClearsDragging()
    {
        var state = DefaultState();
        state.SetTool("move");
        state.PlaceCone(10.0, 20.0);

        var cut = RenderComponent<DiagramCanvas>(
            p => p.Add(x => x.State, state));

        cut.Find("polygon[data-element^='cones']").MouseDown();
        cut.Instance.OnDragComplete(50.0, 60.0);
        cut.Render();

        Assert.Equal(50.0, state.Diagram.Cones[0].X);
        Assert.Equal(60.0, state.Diagram.Cones[0].Y);

        // After OnDragComplete, _draggingRef is cleared — further OnDragMove is a no-op
        cut.Instance.OnDragMove(99.0, 99.0);
        cut.Render();

        Assert.Equal(50.0, state.Diagram.Cones[0].X);
        Assert.Equal(60.0, state.Diagram.Cones[0].Y);
    }
}
