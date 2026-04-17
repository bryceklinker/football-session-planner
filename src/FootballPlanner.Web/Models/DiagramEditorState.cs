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
    }

    public void SetTool(string tool)
    {
        ActiveTool = tool;
        ArrowStartPoint = null;
    }

    public void SetActiveTeam(string teamId) => ActiveTeamId = teamId;

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

    public void BeginDrag() => PushUndo();

    public void PreviewMove(string elementRef, double x, double y)
        => Diagram = ApplyMove(Diagram, elementRef, x, y);

    public void DeleteElement(string elementRef)
    {
        PushUndo();
        Diagram = ApplyDelete(Diagram, elementRef);
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
    }

    public void Undo()
    {
        if (_undoStack.Count == 0) return;
        _redoStack.Push(Diagram);
        Diagram = _undoStack.Pop();
    }

    public void Redo()
    {
        if (_redoStack.Count == 0) return;
        _undoStack.Push(Diagram);
        Diagram = _redoStack.Pop();
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
            "arrows"  => diagram with { Arrows  = ReplaceAt(diagram.Arrows,  idx,
                a => a with { X2 = x, Y2 = y, Cx = (a.X1 + x) / 2.0, Cy = (a.Y1 + y) / 2.0 }) },
            _ => diagram
        };
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

    private static DiagramModel CreateDefault() => new(
        PitchFormat.ElevenVElevenFull, null, null,
        [
            new DiagramTeam("t1", "Red",  "#e94560", []),
            new DiagramTeam("t2", "Blue", "#4169E1", [])
        ],
        [], [], [], []);
}
