namespace FootballPlanner.Domain.Entities;

public class Focus
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;

    private Focus() { }

    public static Focus Create(string name)
    {
        return new Focus { Name = name };
    }

    public void Update(string name)
    {
        Name = name;
    }
}
