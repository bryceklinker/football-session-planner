namespace FootballPlanner.Domain.Entities;

public class Session
{
    public int Id { get; private set; }
    public DateTime Date { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public List<SessionActivity> Activities { get; private set; } = new();

    private Session() { }

    public static Session Create(DateTime date, string title, string? notes)
        => new Session
        {
            Date = date,
            Title = title,
            Notes = notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

    public void Update(DateTime date, string title, string? notes)
    {
        Date = date;
        Title = title;
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }
}
