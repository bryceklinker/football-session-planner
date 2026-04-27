using System.Text.Json.Serialization;

namespace FootballPlanner.Web.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PitchFormat
{
    ElevenVElevenFull, ElevenVElevenHalf,
    NineVNineFull,     NineVNineHalf,
    SevenVSevenFull,   SevenVSevenHalf,
    Custom
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ArrowStyle { Run, Pass, Dribble }

public record DiagramModel(
    PitchFormat PitchFormat,
    double? CustomWidth,
    double? CustomHeight,
    List<DiagramTeam> Teams,
    List<CoachElement> Coaches,
    List<ConeElement> Cones,
    List<GoalElement> Goals,
    List<ArrowElement> Arrows,
    string? Notes = null,
    DiagramLegend? Legend = null);

public record DiagramTeam(
    string Id,
    string Name,
    string Color,
    List<PlayerElement> Players);

public record PlayerElement(string Label, double X, double Y, double Radius = 2.0);
public record CoachElement(string Label, double X, double Y, double Radius = 2.0);
public record ConeElement(double X, double Y, double Size = 1.0, string Color = "#f0a500");
public record GoalElement(double X, double Y, double Width);

public record ArrowElement(
    ArrowStyle Style,
    double X1, double Y1,
    double X2, double Y2,
    double Cx, double Cy,
    string? Color = null,
    int? SequenceNumber = null);

// Stores only position — collapsed/expanded is transient UI state, never persisted.
public record DiagramLegend(double X = 5, double Y = 5);
