namespace FootballPlanner.Domain.Entities;

public class Activity
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string? InspirationUrl { get; private set; }
    public int EstimatedDuration { get; private set; }
    public string? DiagramJson { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Activity() { }

    public static Activity Create(
        string name, string description, string? inspirationUrl, int estimatedDuration)
        => new Activity
        {
            Name = name,
            Description = description,
            InspirationUrl = inspirationUrl,
            EstimatedDuration = estimatedDuration,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

    public void Update(
        string name, string description, string? inspirationUrl, int estimatedDuration)
    {
        Name = name;
        Description = description;
        InspirationUrl = inspirationUrl;
        EstimatedDuration = estimatedDuration;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDiagram(string? diagramJson)
    {
        DiagramJson = diagramJson;
        UpdatedAt = DateTime.UtcNow;
    }
}
