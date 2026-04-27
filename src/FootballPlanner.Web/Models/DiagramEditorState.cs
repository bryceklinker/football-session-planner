namespace FootballPlanner.Web.Models;

public class DiagramEditorState
{
    private const int MaxUndoStackSize = 50;

    private readonly Stack<DiagramModel> _undoStack = new();
    private readonly Stack<DiagramModel> _redoStack = new();

    public DiagramModel Diagram { get; private set; } = CreateDefault();
    public string? ActiveTool { get; private set; }
    public string? ActiveTeamId { get; private set; }
    public (double X, double Y)? ArrowStartPoint { get; private set; }
    public string? SelectedElement { get; private set; }

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public void Initialize(DiagramModel? initial)
    {
        Diagram = initial ?? CreateDefault();
        _undoStack.Clear();
        _redoStack.Clear();
        ActiveTool = null;
        ActiveTeamId = Diagram.Teams.FirstOrDefault()?.Id;
        ArrowStartPoint = null;
        SelectedElement = null;
    }

    public void SelectElement(string? elementRef) => SelectedElement = elementRef;

    // Notes are not pushed to the undo stack — per-keystroke undo entries would be too noisy.
    public void SetNotes(string? notes) => Diagram = Diagram with { Notes = notes };

    public void ResizeElement(string elementRef, double value)
    {
        var parts = elementRef.Split('/');
        if (parts.Length < 2 || !int.TryParse(parts[1], out var idx)) return;
        PushUndo();
        Diagram = parts[0] switch
        {
            "teams" when parts.Length >= 4 && int.TryParse(parts[3], out var pIdx)
                => ApplyResizePlayer(Diagram, idx, pIdx, Math.Clamp(value, 1.0, 5.0)),
            "coaches" => Diagram with { Coaches = ReplaceAt(Diagram.Coaches, idx,
                c => c with { Radius = Math.Clamp(value, 1.0, 5.0) }) },
            "cones" => Diagram with { Cones = ReplaceAt(Diagram.Cones, idx,
                c => c with { Size = Math.Clamp(value, 0.5, 4.0) }) },
            _ => Diagram
        };
    }

    public void ChangeConeColor(string elementRef, string color)
    {
        var parts = elementRef.Split('/');
        if (parts.Length < 2 || parts[0] != "cones" || !int.TryParse(parts[1], out var idx)) return;
        PushUndo();
        Diagram = Diagram with { Cones = ReplaceAt(Diagram.Cones, idx, c => c with { Color = color }) };
    }

    public void ChangeArrowStyle(string elementRef, ArrowStyle style)
    {
        var parts = elementRef.Split('/');
        if (parts.Length < 2 || parts[0] != "arrows" || !int.TryParse(parts[1], out var idx)) return;
        PushUndo();
        Diagram = Diagram with { Arrows = ReplaceAt(Diagram.Arrows, idx, a => a with { Style = style }) };
    }

    public void ChangeArrowColor(string elementRef, string? color)
    {
        var parts = elementRef.Split('/');
        if (parts.Length < 2 || parts[0] != "arrows" || !int.TryParse(parts[1], out var idx)) return;
        PushUndo();
        Diagram = Diagram with { Arrows = ReplaceAt(Diagram.Arrows, idx, a => a with { Color = color }) };
    }

    public void ChangeArrowSequenceNumber(string elementRef, int? number)
    {
        var parts = elementRef.Split('/');
        if (parts.Length < 2 || parts[0] != "arrows" || !int.TryParse(parts[1], out var idx)) return;
        PushUndo();
        Diagram = Diagram with { Arrows = ReplaceAt(Diagram.Arrows, idx, a => a with { SequenceNumber = number }) };
    }

    public void TransferPlayer(string elementRef, string newTeamId)
    {
        var parts = elementRef.Split('/');
        if (parts.Length < 4 || !int.TryParse(parts[1], out var teamIdx) || !int.TryParse(parts[3], out var playerIdx))
            return;
        var currentTeam = Diagram.Teams[teamIdx];
        if (currentTeam.Id == newTeamId) return;
        var newTeamIdx = Diagram.Teams.FindIndex(t => t.Id == newTeamId);
        if (newTeamIdx < 0) return;

        var player = currentTeam.Players[playerIdx];
        var newPlayerIdx = Diagram.Teams[newTeamIdx].Players.Count;

        PushUndo();
        var teams = Diagram.Teams.ToList();
        teams[teamIdx] = currentTeam with { Players = RemoveAt(currentTeam.Players, playerIdx) };
        var newTeam = teams[newTeamIdx];
        teams[newTeamIdx] = newTeam with { Players = [.. newTeam.Players, player] };
        Diagram = Diagram with { Teams = teams };
        SelectedElement = $"teams/{newTeamIdx}/players/{newPlayerIdx}";
    }

    public void SetTool(string tool)
    {
        ActiveTool = ActiveTool == tool ? null : tool;
        ArrowStartPoint = null;
    }

    public void SetActiveTeam(string teamId) => ActiveTeamId = teamId;

    public void SetPitchFormat(PitchFormat format, double? customWidth = null, double? customHeight = null)
    {
        PushUndo();
        Diagram = Diagram with { PitchFormat = format, CustomWidth = customWidth, CustomHeight = customHeight };
    }

    public void PlacePlayer(double x, double y)
    {
        var team = Diagram.Teams.FirstOrDefault(t => t.Id == ActiveTeamId);
        if (team == null) return;
        PushUndo();
        var updated = team with { Players = [.. team.Players, new PlayerElement("", x, y)] };
        Diagram = Diagram with { Teams = Diagram.Teams.Select(t => t.Id == team.Id ? updated : t).ToList() };
    }

    public void PlaceCoach(double x, double y)
    {
        PushUndo();
        Diagram = Diagram with { Coaches = [.. Diagram.Coaches, new CoachElement("C", x, y)] };
    }

    public void PlaceCone(double x, double y)
    {
        PushUndo();
        Diagram = Diagram with { Cones = [.. Diagram.Cones, new ConeElement(x, y)] };
    }

    public void PlaceGoal(double x, double y)
    {
        PushUndo();
        Diagram = Diagram with { Goals = [.. Diagram.Goals, new GoalElement(x, y, 10.0)] };
    }

    public void HandleArrowPoint(double x, double y)
    {
        if (ArrowStartPoint == null)
        {
            ArrowStartPoint = (x, y);
            return;
        }

        var style = ActiveTool switch
        {
            "arrow-pass"    => ArrowStyle.Pass,
            "arrow-dribble" => ArrowStyle.Dribble,
            _               => ArrowStyle.Run
        };
        var (x1, y1) = ArrowStartPoint.Value;
        var cx = (x1 + x) / 2.0;
        var cy = (y1 + y) / 2.0;
        PushUndo();
        Diagram = Diagram with { Arrows = [.. Diagram.Arrows, new ArrowElement(style, x1, y1, x, y, cx, cy)] };
        ArrowStartPoint = null;
    }

    public void MoveElement(string elementRef, double x, double y)
    {
        PushUndo();
        Diagram = ApplyMove(Diagram, elementRef, x, y);
    }

    // Commits a completed drag by translating the element by (dx, dy) in model coordinates.
    // For arrows the entire arrow (both endpoints + control point) is translated together.
    public void MoveByDelta(string elementRef, double dx, double dy)
        => Diagram = ApplyDelta(Diagram, elementRef, dx, dy);

    public void BeginDrag() => PushUndo();

    public (double X, double Y) GetElementPosition(string elementRef)
    {
        var parts = elementRef.Split('/');
        if (parts.Length < 2 || !int.TryParse(parts[1], out var idx)) return (0, 0);
        return parts[0] switch
        {
            "teams" when parts.Length >= 4 && int.TryParse(parts[3], out var pIdx)
                => (Diagram.Teams[idx].Players[pIdx].X, Diagram.Teams[idx].Players[pIdx].Y),
            "coaches" => (Diagram.Coaches[idx].X, Diagram.Coaches[idx].Y),
            "cones"   => (Diagram.Cones[idx].X, Diagram.Cones[idx].Y),
            "goals"   => (Diagram.Goals[idx].X, Diagram.Goals[idx].Y),
            "arrows"  => (Diagram.Arrows[idx].X2, Diagram.Arrows[idx].Y2),
            _ => (0, 0)
        };
    }

    public void PreviewMove(string elementRef, double x, double y)
        => Diagram = ApplyMove(Diagram, elementRef, x, y);

    public void DeleteElement(string elementRef)
    {
        PushUndo();
        Diagram = ApplyDelete(Diagram, elementRef);
        if (SelectedElement == elementRef) SelectedElement = null;
    }

    public void AddTeam(string id, string name, string color)
    {
        PushUndo();
        Diagram = Diagram with { Teams = [.. Diagram.Teams, new DiagramTeam(id, name, color, [])] };
    }

    public void RenameTeam(string teamId, string name)
    {
        PushUndo();
        Diagram = Diagram with { Teams = Diagram.Teams.Select(t => t.Id == teamId ? t with { Name = name } : t).ToList() };
    }

    public void RecolorTeam(string teamId, string color)
    {
        PushUndo();
        Diagram = Diagram with { Teams = Diagram.Teams.Select(t => t.Id == teamId ? t with { Color = color } : t).ToList() };
    }

    public void DeleteTeam(string teamId)
    {
        PushUndo();
        Diagram = Diagram with { Teams = Diagram.Teams.Where(t => t.Id != teamId).ToList() };
        if (ActiveTeamId == teamId)
            ActiveTeamId = Diagram.Teams.FirstOrDefault()?.Id;
    }

    public void Clear()
    {
        PushUndo();
        Diagram = Diagram with
        {
            Coaches = [],
            Cones = [],
            Goals = [],
            Arrows = [],
            Teams = Diagram.Teams.Select(t => t with { Players = [] }).ToList()
        };
        SelectedElement = null;
    }

    public void Undo()
    {
        if (_undoStack.Count == 0) return;
        _redoStack.Push(Diagram);
        Diagram = _undoStack.Pop();
        SelectedElement = null;
    }

    public void Redo()
    {
        if (_redoStack.Count == 0) return;
        _undoStack.Push(Diagram);
        Diagram = _redoStack.Pop();
        SelectedElement = null;
    }

    private void PushUndo()
    {
        _redoStack.Clear();
        _undoStack.Push(Diagram);
        if (_undoStack.Count > MaxUndoStackSize)
        {
            var kept = _undoStack.Take(MaxUndoStackSize).ToArray();
            _undoStack.Clear();
            foreach (var snapshot in kept.Reverse())
                _undoStack.Push(snapshot);
        }
    }

    private static DiagramModel ApplyMove(DiagramModel diagram, string elementRef, double x, double y)
    {
        var parts = elementRef.Split('/');
        if (parts.Length < 2) return diagram;
        if (!int.TryParse(parts[1], out var idx)) return diagram;

        return parts[0] switch
        {
            "teams" when parts.Length >= 4 && int.TryParse(parts[3], out var pIdx)
                => ApplyMovePlayer(diagram, idx, pIdx, x, y),
            "coaches" => diagram with { Coaches = ReplaceAt(diagram.Coaches, idx, c => c with { X = x, Y = y }) },
            "cones"   => diagram with { Cones   = ReplaceAt(diagram.Cones,   idx, c => c with { X = x, Y = y }) },
            "goals"   => diagram with { Goals   = ReplaceAt(diagram.Goals,   idx, g => g with { X = x, Y = y }) },
            // Dragging an arrow repositions its endpoint, redirecting where it points.
            // The start point (X1, Y1) remains fixed; the control point is recomputed as the midpoint.
            "arrows"  => diagram with { Arrows  = ReplaceAt(diagram.Arrows,  idx,
                a => a with { X2 = x, Y2 = y, Cx = (a.X1 + x) / 2.0, Cy = (a.Y1 + y) / 2.0 }) },
            _ => diagram
        };
    }

    private static DiagramModel ApplyResizePlayer(DiagramModel diagram, int teamIdx, int playerIdx, double radius)
    {
        var teams = diagram.Teams.ToList();
        var team = teams[teamIdx];
        teams[teamIdx] = team with { Players = ReplaceAt(team.Players, playerIdx, p => p with { Radius = radius }) };
        return diagram with { Teams = teams };
    }

    private static DiagramModel ApplyMovePlayer(DiagramModel diagram, int teamIdx, int playerIdx, double x, double y)
    {
        var teams = diagram.Teams.ToList();
        var team = teams[teamIdx];
        teams[teamIdx] = team with { Players = ReplaceAt(team.Players, playerIdx, p => p with { X = x, Y = y }) };
        return diagram with { Teams = teams };
    }

    private static DiagramModel ApplyDelete(DiagramModel diagram, string elementRef)
    {
        var parts = elementRef.Split('/');
        if (parts.Length < 2) return diagram;
        if (!int.TryParse(parts[1], out var idx)) return diagram;

        return parts[0] switch
        {
            "teams" when parts.Length >= 4 && int.TryParse(parts[3], out var pIdx)
                => ApplyDeletePlayer(diagram, idx, pIdx),
            "coaches" => diagram with { Coaches = RemoveAt(diagram.Coaches, idx) },
            "cones"   => diagram with { Cones   = RemoveAt(diagram.Cones,   idx) },
            "goals"   => diagram with { Goals   = RemoveAt(diagram.Goals,   idx) },
            "arrows"  => diagram with { Arrows  = RemoveAt(diagram.Arrows,  idx) },
            _ => diagram
        };
    }

    private static DiagramModel ApplyDeletePlayer(DiagramModel diagram, int teamIdx, int playerIdx)
    {
        var teams = diagram.Teams.ToList();
        var team = teams[teamIdx];
        teams[teamIdx] = team with { Players = RemoveAt(team.Players, playerIdx) };
        return diagram with { Teams = teams };
    }

    private static List<T> ReplaceAt<T>(List<T> list, int index, Func<T, T> update)
    {
        var result = list.ToList();
        result[index] = update(result[index]);
        return result;
    }

    private static List<T> RemoveAt<T>(List<T> list, int index)
    {
        var result = list.ToList();
        result.RemoveAt(index);
        return result;
    }

    private static double ClampPos(double v) => Math.Clamp(v, 0, 100);

    private static DiagramModel ApplyDelta(DiagramModel diagram, string elementRef, double dx, double dy)
    {
        var parts = elementRef.Split('/');
        if (parts.Length < 2 || !int.TryParse(parts[1], out var idx)) return diagram;

        return parts[0] switch
        {
            "teams" when parts.Length >= 4 && int.TryParse(parts[3], out var pIdx)
                => ApplyDeltaPlayer(diagram, idx, pIdx, dx, dy),
            "coaches" => diagram with { Coaches = ReplaceAt(diagram.Coaches, idx,
                c => c with { X = ClampPos(c.X + dx), Y = ClampPos(c.Y + dy) }) },
            "cones"   => diagram with { Cones   = ReplaceAt(diagram.Cones,   idx,
                c => c with { X = ClampPos(c.X + dx), Y = ClampPos(c.Y + dy) }) },
            "goals"   => diagram with { Goals   = ReplaceAt(diagram.Goals,   idx,
                g => g with { X = ClampPos(g.X + dx), Y = ClampPos(g.Y + dy) }) },
            // Move entire arrow — translate all points by the same delta.
            "arrows"  => diagram with { Arrows  = ReplaceAt(diagram.Arrows,  idx,
                a => a with {
                    X1 = ClampPos(a.X1 + dx), Y1 = ClampPos(a.Y1 + dy),
                    X2 = ClampPos(a.X2 + dx), Y2 = ClampPos(a.Y2 + dy),
                    Cx = ClampPos(a.Cx + dx), Cy = ClampPos(a.Cy + dy)
                }) },
            _ => diagram
        };
    }

    private static DiagramModel ApplyDeltaPlayer(DiagramModel diagram, int teamIdx, int playerIdx, double dx, double dy)
    {
        var teams = diagram.Teams.ToList();
        var team = teams[teamIdx];
        teams[teamIdx] = team with { Players = ReplaceAt(team.Players, playerIdx,
            p => p with { X = ClampPos(p.X + dx), Y = ClampPos(p.Y + dy) }) };
        return diagram with { Teams = teams };
    }

    private static DiagramModel CreateDefault() => new(
        PitchFormat.ElevenVElevenFull, null, null,
        [
            new DiagramTeam("t1", "Red",  "#e94560", []),
            new DiagramTeam("t2", "Blue", "#4169E1", [])
        ],
        [], [], [], []);
}
