using FootballPlanner.Web.Models;

namespace FootballPlanner.Web.Tests;

public class DiagramEditorStateTests
{
    // HandleArrowPoint does not require an active tool — style defaults to Run.
    private static DiagramEditorState StateWithOneArrow()
    {
        var state = new DiagramEditorState();
        state.Initialize(null);
        state.HandleArrowPoint(10, 20);
        state.HandleArrowPoint(80, 60);
        return state;
    }

    // ── ChangeArrowStyle ──────────────────────────────────────────────────────

    [Fact]
    public void ChangeArrowStyle_UpdatesStyle()
    {
        var state = StateWithOneArrow();
        state.ChangeArrowStyle("arrows/0", ArrowStyle.Pass);
        Assert.Equal(ArrowStyle.Pass, state.Diagram.Arrows[0].Style);
    }

    [Fact]
    public void ChangeArrowStyle_PushesUndo()
    {
        var state = StateWithOneArrow();
        state.ChangeArrowStyle("arrows/0", ArrowStyle.Dribble);
        state.Undo();
        Assert.Equal(ArrowStyle.Run, state.Diagram.Arrows[0].Style);
    }

    [Fact]
    public void ChangeArrowStyle_InvalidRef_DoesNothing()
    {
        var state = StateWithOneArrow();
        state.ChangeArrowStyle("cones/0", ArrowStyle.Pass);
        Assert.Equal(ArrowStyle.Run, state.Diagram.Arrows[0].Style);
    }

    // ── ChangeArrowColor ──────────────────────────────────────────────────────

    [Fact]
    public void ChangeArrowColor_UpdatesColor()
    {
        var state = StateWithOneArrow();
        state.ChangeArrowColor("arrows/0", "#ff0000");
        Assert.Equal("#ff0000", state.Diagram.Arrows[0].Color);
    }

    [Fact]
    public void ChangeArrowColor_Null_ClearsColor()
    {
        var state = StateWithOneArrow();
        state.ChangeArrowColor("arrows/0", "#ff0000");
        state.ChangeArrowColor("arrows/0", null);
        Assert.Null(state.Diagram.Arrows[0].Color);
    }

    [Fact]
    public void ChangeArrowColor_PushesUndo()
    {
        var state = StateWithOneArrow();
        state.ChangeArrowColor("arrows/0", "#ff0000");
        state.Undo();
        Assert.Null(state.Diagram.Arrows[0].Color);
    }

    // ── ChangeArrowSequenceNumber ─────────────────────────────────────────────

    [Fact]
    public void ChangeArrowSequenceNumber_UpdatesNumber()
    {
        var state = StateWithOneArrow();
        state.ChangeArrowSequenceNumber("arrows/0", 2);
        Assert.Equal(2, state.Diagram.Arrows[0].SequenceNumber);
    }

    [Fact]
    public void ChangeArrowSequenceNumber_Null_ClearsNumber()
    {
        var state = StateWithOneArrow();
        state.ChangeArrowSequenceNumber("arrows/0", 3);
        state.ChangeArrowSequenceNumber("arrows/0", null);
        Assert.Null(state.Diagram.Arrows[0].SequenceNumber);
    }

    [Fact]
    public void ChangeArrowSequenceNumber_PushesUndo()
    {
        var state = StateWithOneArrow();
        state.ChangeArrowSequenceNumber("arrows/0", 1);
        state.Undo();
        Assert.Null(state.Diagram.Arrows[0].SequenceNumber);
    }
}
