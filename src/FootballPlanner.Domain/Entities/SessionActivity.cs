namespace FootballPlanner.Domain.Entities;

public class SessionActivity
{
    public int Id { get; private set; }
    public int SessionId { get; private set; }
    public int ActivityId { get; private set; }
    public int PhaseId { get; private set; }
    public int FocusId { get; private set; }
    public int Duration { get; private set; }
    public int DisplayOrder { get; private set; }
    public string? Notes { get; private set; }

    public Activity Activity { get; private set; } = null!;
    public Phase Phase { get; private set; } = null!;
    public Focus Focus { get; private set; } = null!;
    public List<SessionActivityKeyPoint> KeyPoints { get; private set; } = new();

    private SessionActivity() { }

    public static SessionActivity Create(
        int sessionId, int activityId, int phaseId, int focusId,
        int duration, int displayOrder, string? notes)
        => new SessionActivity
        {
            SessionId = sessionId,
            ActivityId = activityId,
            PhaseId = phaseId,
            FocusId = focusId,
            Duration = duration,
            DisplayOrder = displayOrder,
            Notes = notes,
        };

    public void Update(int phaseId, int focusId, int duration, string? notes)
    {
        PhaseId = phaseId;
        FocusId = focusId;
        Duration = duration;
        Notes = notes;
    }
}
