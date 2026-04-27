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

    // ── MoveByDelta arrow handles ─────────────────────────────────────────────

    [Fact]
    public void MoveByDelta_ArrowTailHandle_MovesStartPointAndControlPoint()
    {
        var state = StateWithOneArrow();
        var before = state.Diagram.Arrows[0];
        state.MoveByDelta("arrows/0/tail", 5.0, 3.0);
        var after = state.Diagram.Arrows[0];

        Assert.Equal(before.X1 + 5.0, after.X1, precision: 6);
        Assert.Equal(before.Y1 + 3.0, after.Y1, precision: 6);
        // Control point translates by same delta
        Assert.Equal(before.Cx + 5.0, after.Cx, precision: 6);
        Assert.Equal(before.Cy + 3.0, after.Cy, precision: 6);
        // End point unchanged
        Assert.Equal(before.X2, after.X2, precision: 6);
        Assert.Equal(before.Y2, after.Y2, precision: 6);
    }

    [Fact]
    public void MoveByDelta_ArrowTipHandle_MovesEndPointAndControlPoint()
    {
        var state = StateWithOneArrow();
        var before = state.Diagram.Arrows[0];
        state.MoveByDelta("arrows/0/tip", 4.0, -2.0);
        var after = state.Diagram.Arrows[0];

        Assert.Equal(before.X2 + 4.0, after.X2, precision: 6);
        Assert.Equal(before.Y2 - 2.0, after.Y2, precision: 6);
        // Control point translates by same delta
        Assert.Equal(before.Cx + 4.0, after.Cx, precision: 6);
        Assert.Equal(before.Cy - 2.0, after.Cy, precision: 6);
        // Start point unchanged
        Assert.Equal(before.X1, after.X1, precision: 6);
        Assert.Equal(before.Y1, after.Y1, precision: 6);
    }

    [Fact]
    public void MoveByDelta_ArrowCurveHandle_MovesControlPointOnly()
    {
        var state = StateWithOneArrow();
        var before = state.Diagram.Arrows[0];
        state.MoveByDelta("arrows/0/curve", 2.0, 8.0);
        var after = state.Diagram.Arrows[0];

        Assert.Equal(before.Cx + 2.0, after.Cx, precision: 6);
        Assert.Equal(before.Cy + 8.0, after.Cy, precision: 6);
        // Endpoints unchanged
        Assert.Equal(before.X1, after.X1, precision: 6);
        Assert.Equal(before.Y1, after.Y1, precision: 6);
        Assert.Equal(before.X2, after.X2, precision: 6);
        Assert.Equal(before.Y2, after.Y2, precision: 6);
    }

    // ── Legend ────────────────────────────────────────────────────────────────

    [Fact]
    public void AddLegend_CreatesLegendAtDefaultPosition()
    {
        var state = new DiagramEditorState();
        state.Initialize(null);
        state.AddLegend();
        Assert.NotNull(state.Diagram.Legend);
        Assert.Equal(5.0, state.Diagram.Legend!.X, precision: 6);
        Assert.Equal(5.0, state.Diagram.Legend!.Y, precision: 6);
    }

    [Fact]
    public void AddLegend_PushesUndo()
    {
        var state = new DiagramEditorState();
        state.Initialize(null);
        state.AddLegend();
        state.Undo();
        Assert.Null(state.Diagram.Legend);
    }

    [Fact]
    public void AddLegend_ClearsSelectedElement()
    {
        var state = StateWithOneArrow();
        state.SelectElement("arrows/0");
        state.AddLegend();
        Assert.Null(state.SelectedElement);
    }

    [Fact]
    public void RemoveLegend_SetsLegendToNull()
    {
        var state = new DiagramEditorState();
        state.Initialize(null);
        state.AddLegend();
        state.RemoveLegend();
        Assert.Null(state.Diagram.Legend);
    }

    [Fact]
    public void RemoveLegend_PushesUndo()
    {
        var state = new DiagramEditorState();
        state.Initialize(null);
        state.AddLegend();
        state.RemoveLegend();
        state.Undo();
        Assert.NotNull(state.Diagram.Legend);
    }

    [Fact]
    public void RemoveLegend_WhenLegendSelected_ClearsSelectedElement()
    {
        var state = new DiagramEditorState();
        state.Initialize(null);
        state.AddLegend();
        state.SelectElement("legend");
        state.RemoveLegend();
        Assert.Null(state.SelectedElement);
    }
}
