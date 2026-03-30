namespace FootballPlanner.Infra;

public class Naming(string environment)
{
    public string Environment { get; } = environment;

    public string Resource(string baseName) => $"{baseName}-{Environment}";
}
