namespace FootballPlanner.Domain.Entities;

public class SessionActivityKeyPoint
{
    public int Id { get; private set; }
    public int SessionActivityId { get; private set; }
    public int Order { get; private set; }
    public string Text { get; private set; } = string.Empty;

    private SessionActivityKeyPoint() { }

    public static SessionActivityKeyPoint Create(int sessionActivityId, int order, string text)
        => new SessionActivityKeyPoint
        {
            SessionActivityId = sessionActivityId,
            Order = order,
            Text = text,
        };
}
