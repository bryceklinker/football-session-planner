namespace FootballPlanner.Domain.Entities;

public class Phase
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int Order { get; private set; }

    private Phase() { }

    public static Phase Create(string name, int order)
    {
        return new Phase { Name = name, Order = order };
    }

    public void Update(string name, int order)
    {
        Name = name;
        Order = order;
    }
}
