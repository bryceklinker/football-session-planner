using FootballPlanner.Web.Models;

namespace FootballPlanner.Component.Tests.Models;

public class DiagramEditorStateTests
{
    [Fact]
    public void PlacePlayer_AddsPlayerToActiveTeam()
    {
        var state = new DiagramEditorState();
        state.SetTool("player");
        state.SetActiveTeam("t1");

        state.PlacePlayer(25.0, 60.0);

        var players = state.Diagram.Teams.First(t => t.Id == "t1").Players;
        Assert.Single(players);
        Assert.Equal(25.0, players[0].X);
        Assert.Equal(60.0, players[0].Y);
    }

    [Fact]
    public void PlacePlayer_WithNoActiveTeam_DoesNothing()
    {
        var state = new DiagramEditorState();
        state.SetTool("player");
        // no SetActiveTeam called — ActiveTeamId is set to first team on Initialize,
        // so explicitly clear it
        state.Initialize(new DiagramModel(
            PitchFormat.ElevenVElevenFull, null, null, [], [], [], [], []));

        state.PlacePlayer(50.0, 50.0);

        Assert.All(state.Diagram.Teams, t => Assert.Empty(t.Players));
    }

    [Fact]
    public void PlaceCoach_AddsCoach()
    {
        var state = new DiagramEditorState();

        state.PlaceCoach(50.0, 10.0);

        Assert.Single(state.Diagram.Coaches);
        Assert.Equal("C", state.Diagram.Coaches[0].Label);
        Assert.Equal(50.0, state.Diagram.Coaches[0].X);
    }

    [Fact]
    public void PlaceCone_AddsCone()
    {
        var state = new DiagramEditorState();

        state.PlaceCone(30.0, 40.0);

        Assert.Single(state.Diagram.Cones);
    }

    [Fact]
    public void PlaceGoal_AddsGoal()
    {
        var state = new DiagramEditorState();

        state.PlaceGoal(10.0, 50.0);

        Assert.Single(state.Diagram.Goals);
        Assert.Equal(10.0, state.Diagram.Goals[0].Width);
    }

    [Fact]
    public void HandleArrowPoint_FirstClick_SetsStartPoint()
    {
        var state = new DiagramEditorState();
        state.SetTool("arrow-run");

        state.HandleArrowPoint(20.0, 30.0);

        Assert.NotNull(state.ArrowStartPoint);
        Assert.Equal(20.0, state.ArrowStartPoint!.Value.X);
        Assert.Equal(30.0, state.ArrowStartPoint!.Value.Y);
        Assert.Empty(state.Diagram.Arrows);
    }

    [Fact]
    public void HandleArrowPoint_SecondClick_CreatesArrowAndClearsStart()
    {
        var state = new DiagramEditorState();
        state.SetTool("arrow-pass");

        state.HandleArrowPoint(10.0, 20.0);
        state.HandleArrowPoint(80.0, 70.0);

        Assert.Null(state.ArrowStartPoint);
        Assert.Single(state.Diagram.Arrows);
        var arrow = state.Diagram.Arrows[0];
        Assert.Equal(ArrowStyle.Pass, arrow.Style);
        Assert.Equal(10.0, arrow.X1);
        Assert.Equal(20.0, arrow.Y1);
        Assert.Equal(80.0, arrow.X2);
        Assert.Equal(70.0, arrow.Y2);
        Assert.Equal(45.0, arrow.Cx); // midpoint of X1(10) and X2(80)
        Assert.Equal(45.0, arrow.Cy); // midpoint of Y1(20) and Y2(70)
    }

    [Fact]
    public void Undo_RestoresPreviousState()
    {
        var state = new DiagramEditorState();
        state.PlaceCone(30.0, 40.0);

        state.Undo();

        Assert.Empty(state.Diagram.Cones);
    }

    [Fact]
    public void Redo_ReappliesUndoneState()
    {
        var state = new DiagramEditorState();
        state.PlaceCone(30.0, 40.0);
        state.Undo();

        state.Redo();

        Assert.Single(state.Diagram.Cones);
    }

    [Fact]
    public void NewMutation_ClearsRedoStack()
    {
        var state = new DiagramEditorState();
        state.PlaceCone(30.0, 40.0);
        state.Undo();
        Assert.True(state.CanRedo);

        state.PlaceCone(50.0, 50.0); // new mutation

        Assert.False(state.CanRedo);
    }

    [Fact]
    public void UndoStack_DoesNotExceed50Snapshots()
    {
        var state = new DiagramEditorState();
        for (var i = 0; i < 60; i++)
            state.PlaceCone(i, i);

        // Undo 50 times should succeed; 51st should do nothing
        for (var i = 0; i < 50; i++)
            state.Undo();

        Assert.False(state.CanUndo);
    }

    [Fact]
    public void Clear_RemovesAllElementsButPreservesTeams()
    {
        var state = new DiagramEditorState();
        state.SetActiveTeam("t1");
        state.PlacePlayer(50.0, 50.0);
        state.PlaceCone(30.0, 30.0);

        state.Clear();

        Assert.Empty(state.Diagram.Cones);
        Assert.All(state.Diagram.Teams, t => Assert.Empty(t.Players));
        Assert.Equal(2, state.Diagram.Teams.Count); // default teams preserved
    }

    [Fact]
    public void DeleteTeam_RemovesTeamAndItsPlayers()
    {
        var state = new DiagramEditorState();
        state.SetActiveTeam("t1");
        state.PlacePlayer(50.0, 50.0);

        state.DeleteTeam("t1");

        Assert.DoesNotContain(state.Diagram.Teams, t => t.Id == "t1");
    }

    [Fact]
    public void RecolorTeam_UpdatesColor()
    {
        var state = new DiagramEditorState();

        state.RecolorTeam("t1", "#ff0000");

        Assert.Equal("#ff0000", state.Diagram.Teams.First(t => t.Id == "t1").Color);
    }

    [Fact]
    public void AddTeam_AddsTeamWithGivenProperties()
    {
        var state = new DiagramEditorState();

        state.AddTeam("t3", "Yellow", "#ffff00");

        Assert.Equal(3, state.Diagram.Teams.Count);
        var added = state.Diagram.Teams.Last();
        Assert.Equal("t3", added.Id);
        Assert.Equal("Yellow", added.Name);
        Assert.Equal("#ffff00", added.Color);
    }

    [Fact]
    public void MoveElement_UpdatesConePosition()
    {
        var state = new DiagramEditorState();
        state.PlaceCone(10.0, 10.0);

        state.MoveElement("cones/0", 50.0, 60.0);

        Assert.Equal(50.0, state.Diagram.Cones[0].X);
        Assert.Equal(60.0, state.Diagram.Cones[0].Y);
    }

    [Fact]
    public void DeleteElement_RemovesCone()
    {
        var state = new DiagramEditorState();
        state.PlaceCone(10.0, 10.0);
        state.PlaceCone(20.0, 20.0);

        state.DeleteElement("cones/0");

        Assert.Single(state.Diagram.Cones);
        Assert.Equal(20.0, state.Diagram.Cones[0].X);
    }

    [Fact]
    public void MovePlayer_UpdatesPlayerPosition()
    {
        var state = new DiagramEditorState();
        state.SetActiveTeam("t1");
        state.PlacePlayer(10.0, 10.0);

        state.MoveElement("teams/0/players/0", 75.0, 80.0);

        Assert.Equal(75.0, state.Diagram.Teams[0].Players[0].X);
        Assert.Equal(80.0, state.Diagram.Teams[0].Players[0].Y);
    }

    [Fact]
    public void Initialize_WithNull_SetsDefaultDiagramWithTwoTeams()
    {
        var state = new DiagramEditorState();
        state.PlaceCone(10.0, 10.0); // add something

        state.Initialize(null);

        Assert.Empty(state.Diagram.Cones);
        Assert.Equal(2, state.Diagram.Teams.Count);
        Assert.False(state.CanUndo);
    }

    [Fact]
    public void BeginDrag_PushesUndoSnapshot()
    {
        var state = new DiagramEditorState();
        Assert.False(state.CanUndo); // baseline — nothing pushed yet
        state.BeginDrag();
        Assert.True(state.CanUndo);
    }

    [Fact]
    public void PreviewMove_UpdatesPositionWithoutAddingToUndoStack()
    {
        var state = new DiagramEditorState();
        state.PlaceCone(10.0, 20.0);
        // Reset undo stack to a known state
        state.Initialize(state.Diagram);
        state.PlaceCone(10.0, 20.0); // one cone at index 0, one undo entry

        state.PreviewMove("cones/0", 50.0, 60.0);

        // Position updated
        Assert.Equal(50.0, state.Diagram.Cones[0].X);
        Assert.Equal(60.0, state.Diagram.Cones[0].Y);
        // Undo stack NOT grown — still only the one entry from PlaceCone
        Assert.True(state.CanUndo);
        state.Undo();
        Assert.False(state.CanUndo); // no further entries
    }

    [Fact]
    public void SetTool_WhenInactive_ActivatesTool()
    {
        var state = new DiagramEditorState();
        state.SetTool("player");
        Assert.Equal("player", state.ActiveTool);
    }

    [Fact]
    public void SetTool_WhenAlreadyActive_DeactivatesTool()
    {
        var state = new DiagramEditorState();
        state.SetTool("player");
        state.SetTool("player");
        Assert.Null(state.ActiveTool);
    }

    [Fact]
    public void SetPitchFormat_UpdatesDiagramPitchFormat()
    {
        var state = new DiagramEditorState();
        state.SetPitchFormat(PitchFormat.SevenVSevenFull);
        Assert.Equal(PitchFormat.SevenVSevenFull, state.Diagram.PitchFormat);
    }

    [Fact]
    public void SetPitchFormat_IsUndoable()
    {
        var state = new DiagramEditorState();
        Assert.False(state.CanUndo);
        state.SetPitchFormat(PitchFormat.NineVNineFull);
        Assert.True(state.CanUndo);
        state.Undo();
        Assert.Equal(PitchFormat.ElevenVElevenFull, state.Diagram.PitchFormat);
    }

    [Fact]
    public void SetPitchFormat_Custom_SetsWidthAndHeight()
    {
        var state = new DiagramEditorState();
        state.SetPitchFormat(PitchFormat.Custom, customWidth: 80.0, customHeight: 50.0);
        Assert.Equal(PitchFormat.Custom, state.Diagram.PitchFormat);
        Assert.Equal(80.0, state.Diagram.CustomWidth);
        Assert.Equal(50.0, state.Diagram.CustomHeight);
    }

    // ── MoveByDelta ───────────────────────────────────────────────────────────

    [Fact]
    public void MoveByDelta_Cone_TranslatesByDelta()
    {
        var state = new DiagramEditorState();
        state.PlaceCone(30.0, 40.0);

        state.MoveByDelta("cones/0", 10.0, 5.0);

        Assert.Equal(40.0, state.Diagram.Cones[0].X);
        Assert.Equal(45.0, state.Diagram.Cones[0].Y);
    }

    [Fact]
    public void MoveByDelta_Player_TranslatesByDelta()
    {
        var state = new DiagramEditorState();
        state.SetActiveTeam("t1");
        state.PlacePlayer(20.0, 30.0);

        state.MoveByDelta("teams/0/players/0", 5.0, 10.0);

        Assert.Equal(25.0, state.Diagram.Teams[0].Players[0].X);
        Assert.Equal(40.0, state.Diagram.Teams[0].Players[0].Y);
    }

    [Fact]
    public void MoveByDelta_Arrow_TranslatesAllPoints()
    {
        var state = new DiagramEditorState();
        state.SetTool("arrow-run");
        state.HandleArrowPoint(10.0, 20.0);
        state.HandleArrowPoint(50.0, 60.0);

        state.MoveByDelta("arrows/0", 5.0, 5.0);

        var arrow = state.Diagram.Arrows[0];
        Assert.Equal(15.0, arrow.X1);
        Assert.Equal(25.0, arrow.Y1);
        Assert.Equal(55.0, arrow.X2);
        Assert.Equal(65.0, arrow.Y2);
        Assert.Equal(35.0, arrow.Cx); // original midpoint 30 + 5
        Assert.Equal(45.0, arrow.Cy); // original midpoint 40 + 5
    }

    [Fact]
    public void MoveByDelta_ClampsToZeroMin()
    {
        var state = new DiagramEditorState();
        state.PlaceCone(5.0, 5.0);

        state.MoveByDelta("cones/0", -20.0, -20.0);

        Assert.Equal(0.0, state.Diagram.Cones[0].X);
        Assert.Equal(0.0, state.Diagram.Cones[0].Y);
    }

    [Fact]
    public void MoveByDelta_ClampsTo100Max()
    {
        var state = new DiagramEditorState();
        state.PlaceCone(95.0, 95.0);

        state.MoveByDelta("cones/0", 20.0, 20.0);

        Assert.Equal(100.0, state.Diagram.Cones[0].X);
        Assert.Equal(100.0, state.Diagram.Cones[0].Y);
    }

    [Fact]
    public void BeginDragThenMoveByDelta_IsUndoableAsOneStep()
    {
        var state = new DiagramEditorState();
        state.PlaceCone(10.0, 20.0);
        state.Initialize(state.Diagram); // clear undo history, keep cone

        state.BeginDrag();
        state.MoveByDelta("cones/0", 40.0, 40.0);

        state.Undo(); // should revert to pre-drag position

        Assert.Equal(10.0, state.Diagram.Cones[0].X);
        Assert.Equal(20.0, state.Diagram.Cones[0].Y);
        Assert.False(state.CanUndo); // only one undo entry (from BeginDrag)
    }

    // ── Arrow styles ──────────────────────────────────────────────────────────

    [Fact]
    public void HandleArrowPoint_DribbleTool_CreatesDribbleArrow()
    {
        var state = new DiagramEditorState();
        state.SetTool("arrow-dribble");
        state.HandleArrowPoint(10.0, 20.0);
        state.HandleArrowPoint(80.0, 70.0);
        Assert.Equal(ArrowStyle.Dribble, state.Diagram.Arrows[0].Style);
    }

    [Fact]
    public void HandleArrowPoint_RunTool_CreatesRunArrow()
    {
        var state = new DiagramEditorState();
        state.SetTool("arrow-run");
        state.HandleArrowPoint(10.0, 20.0);
        state.HandleArrowPoint(80.0, 70.0);
        Assert.Equal(ArrowStyle.Run, state.Diagram.Arrows[0].Style);
    }

    [Fact]
    public void SetTool_ClearsArrowStartPoint()
    {
        var state = new DiagramEditorState();
        state.SetTool("arrow-run");
        state.HandleArrowPoint(10.0, 20.0);
        Assert.NotNull(state.ArrowStartPoint);

        state.SetTool("cone");

        Assert.Null(state.ArrowStartPoint);
    }

    // ── DeleteElement ─────────────────────────────────────────────────────────

    [Fact]
    public void DeleteElement_RemovesPlayer()
    {
        var state = new DiagramEditorState();
        state.SetActiveTeam("t1");
        state.PlacePlayer(10.0, 20.0);

        state.DeleteElement("teams/0/players/0");

        Assert.Empty(state.Diagram.Teams[0].Players);
    }

    [Fact]
    public void DeleteElement_RemovesArrow()
    {
        var state = new DiagramEditorState();
        state.SetTool("arrow-run");
        state.HandleArrowPoint(10.0, 20.0);
        state.HandleArrowPoint(80.0, 70.0);

        state.DeleteElement("arrows/0");

        Assert.Empty(state.Diagram.Arrows);
    }

    [Fact]
    public void DeleteElement_IsUndoable()
    {
        var state = new DiagramEditorState();
        state.PlaceCone(10.0, 20.0);
        state.DeleteElement("cones/0");

        state.Undo();

        Assert.Single(state.Diagram.Cones);
    }

    // ── Team management ───────────────────────────────────────────────────────

    [Fact]
    public void RenameTeam_ChangesTeamName()
    {
        var state = new DiagramEditorState();

        state.RenameTeam("t1", "Tigers");

        Assert.Equal("Tigers", state.Diagram.Teams.First(t => t.Id == "t1").Name);
    }

    [Fact]
    public void DeleteTeam_WhenActiveTeam_SwitchesActiveTeamToAnother()
    {
        var state = new DiagramEditorState();
        state.SetActiveTeam("t1");

        state.DeleteTeam("t1");

        Assert.NotEqual("t1", state.ActiveTeamId);
        // t2 remains, so ActiveTeamId should not be null
        Assert.NotNull(state.ActiveTeamId);
    }

    // ── Selection ─────────────────────────────────────────────────────────────

    [Fact]
    public void SelectElement_SetsSelectedElement()
    {
        var state = new DiagramEditorState();
        state.PlaceCone(50.0, 50.0);

        state.SelectElement("cones/0");

        Assert.Equal("cones/0", state.SelectedElement);
    }

    [Fact]
    public void SelectElement_Null_ClearsSelection()
    {
        var state = new DiagramEditorState();
        state.SelectElement("cones/0");

        state.SelectElement(null);

        Assert.Null(state.SelectedElement);
    }

    [Fact]
    public void DeleteElement_WhenSelectedElement_ClearsSelection()
    {
        var state = new DiagramEditorState();
        state.PlaceCone(50.0, 50.0);
        state.SelectElement("cones/0");

        state.DeleteElement("cones/0");

        Assert.Null(state.SelectedElement);
    }

    [Fact]
    public void Undo_ClearsSelectedElement()
    {
        var state = new DiagramEditorState();
        state.PlaceCone(50.0, 50.0);
        state.SelectElement("cones/0");

        state.Undo();

        Assert.Null(state.SelectedElement);
    }

    // ── Notes ─────────────────────────────────────────────────────────────────

    [Fact]
    public void SetNotes_UpdatesNotes()
    {
        var state = new DiagramEditorState();

        state.SetNotes("Keep compact, 10 min max");

        Assert.Equal("Keep compact, 10 min max", state.Diagram.Notes);
    }

    [Fact]
    public void SetNotes_DoesNotPushToUndoStack()
    {
        var state = new DiagramEditorState();

        state.SetNotes("first");
        state.SetNotes("second");

        Assert.False(state.CanUndo);
    }

    // ── Resize ────────────────────────────────────────────────────────────────

    [Fact]
    public void ResizeElement_Player_UpdatesRadius()
    {
        var state = new DiagramEditorState();
        state.SetActiveTeam("t1");
        state.PlacePlayer(50.0, 50.0);

        state.ResizeElement("teams/0/players/0", 3.5);

        Assert.Equal(3.5, state.Diagram.Teams[0].Players[0].Radius);
    }

    [Fact]
    public void ResizeElement_Coach_UpdatesRadius()
    {
        var state = new DiagramEditorState();
        state.PlaceCoach(50.0, 50.0);

        state.ResizeElement("coaches/0", 4.0);

        Assert.Equal(4.0, state.Diagram.Coaches[0].Radius);
    }

    [Fact]
    public void ResizeElement_Cone_UpdatesSize()
    {
        var state = new DiagramEditorState();
        state.PlaceCone(50.0, 50.0);

        state.ResizeElement("cones/0", 2.0);

        Assert.Equal(2.0, state.Diagram.Cones[0].Size);
    }

    [Fact]
    public void ResizeElement_ClampsToMinAndMax()
    {
        var state = new DiagramEditorState();
        state.PlaceCone(50.0, 50.0);
        state.SetActiveTeam("t1");
        state.PlacePlayer(30.0, 30.0);

        state.ResizeElement("cones/0", 0.1); // below min 0.5
        state.ResizeElement("teams/0/players/0", 99.0); // above max 5.0

        Assert.Equal(0.5, state.Diagram.Cones[0].Size);
        Assert.Equal(5.0, state.Diagram.Teams[0].Players[0].Radius);
    }

    [Fact]
    public void ResizeElement_IsUndoable()
    {
        var state = new DiagramEditorState();
        state.PlaceCone(50.0, 50.0);

        state.ResizeElement("cones/0", 3.0);
        state.Undo();

        Assert.Equal(1.0, state.Diagram.Cones[0].Size);
    }

    // ── Cone colour ───────────────────────────────────────────────────────────

    [Fact]
    public void ChangeConeColor_UpdatesColor()
    {
        var state = new DiagramEditorState();
        state.PlaceCone(50.0, 50.0);

        state.ChangeConeColor("cones/0", "#e94560");

        Assert.Equal("#e94560", state.Diagram.Cones[0].Color);
    }

    [Fact]
    public void ChangeConeColor_IsUndoable()
    {
        var state = new DiagramEditorState();
        state.PlaceCone(50.0, 50.0);

        state.ChangeConeColor("cones/0", "#e94560");
        state.Undo();

        Assert.Equal("#f0a500", state.Diagram.Cones[0].Color);
    }

    // ── TransferPlayer ────────────────────────────────────────────────────────

    [Fact]
    public void TransferPlayer_MovesPlayerToNewTeam()
    {
        var state = new DiagramEditorState();
        state.SetActiveTeam("t1");
        state.PlacePlayer(30.0, 30.0);

        state.TransferPlayer("teams/0/players/0", "t2");

        Assert.Empty(state.Diagram.Teams[0].Players);
        Assert.Single(state.Diagram.Teams[1].Players);
    }

    [Fact]
    public void TransferPlayer_UpdatesSelectedElement()
    {
        var state = new DiagramEditorState();
        state.SetActiveTeam("t1");
        state.PlacePlayer(30.0, 30.0);
        state.SelectElement("teams/0/players/0");

        state.TransferPlayer("teams/0/players/0", "t2");

        Assert.Equal("teams/1/players/0", state.SelectedElement);
    }

    [Fact]
    public void TransferPlayer_IsUndoable()
    {
        var state = new DiagramEditorState();
        state.SetActiveTeam("t1");
        state.PlacePlayer(30.0, 30.0);

        state.TransferPlayer("teams/0/players/0", "t2");
        state.Undo();

        Assert.Single(state.Diagram.Teams[0].Players);
        Assert.Empty(state.Diagram.Teams[1].Players);
    }
}
