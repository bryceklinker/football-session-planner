using System.Text.Json;
using System.Text.Json.Serialization;
using FootballPlanner.Web.Models;

namespace FootballPlanner.Component.Tests.Models;

public class DiagramModelSerializationTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public void PlayerElement_RoundTrips()
    {
        var model = new DiagramModel(
            PitchFormat.ElevenVElevenFull, null, null,
            [new DiagramTeam("t1", "Red", "#e94560", [new PlayerElement("9", 25.5, 60.0)])],
            [], [], [], []);

        var json = JsonSerializer.Serialize(model, Options);
        var result = JsonSerializer.Deserialize<DiagramModel>(json, Options)!;

        Assert.Equal("9", result.Teams[0].Players[0].Label);
        Assert.Equal(25.5, result.Teams[0].Players[0].X);
        Assert.Equal(60.0, result.Teams[0].Players[0].Y);
    }

    [Fact]
    public void CoachElement_RoundTrips()
    {
        var model = new DiagramModel(
            PitchFormat.NineVNineFull, null, null, [],
            [new CoachElement("Coach", 50.0, 10.0)],
            [], [], []);

        var json = JsonSerializer.Serialize(model, Options);
        var result = JsonSerializer.Deserialize<DiagramModel>(json, Options)!;

        Assert.Equal("Coach", result.Coaches[0].Label);
        Assert.Equal(50.0, result.Coaches[0].X);
    }

    [Fact]
    public void ConeElement_RoundTrips()
    {
        var model = new DiagramModel(
            PitchFormat.SevenVSevenFull, null, null, [], [],
            [new ConeElement(30.0, 40.0)],
            [], []);

        var json = JsonSerializer.Serialize(model, Options);
        var result = JsonSerializer.Deserialize<DiagramModel>(json, Options)!;

        Assert.Equal(30.0, result.Cones[0].X);
        Assert.Equal(40.0, result.Cones[0].Y);
    }

    [Fact]
    public void GoalElement_RoundTrips()
    {
        var model = new DiagramModel(
            PitchFormat.ElevenVElevenHalf, null, null, [], [], [],
            [new GoalElement(10.0, 50.0, 15.0)],
            []);

        var json = JsonSerializer.Serialize(model, Options);
        var result = JsonSerializer.Deserialize<DiagramModel>(json, Options)!;

        Assert.Equal(15.0, result.Goals[0].Width);
    }

    [Theory]
    [InlineData(ArrowStyle.Run)]
    [InlineData(ArrowStyle.Pass)]
    [InlineData(ArrowStyle.Dribble)]
    public void ArrowElement_RoundTrips_AllStyles(ArrowStyle style)
    {
        var model = new DiagramModel(
            PitchFormat.ElevenVElevenFull, null, null, [], [], [], [],
            [new ArrowElement(style, 10.0, 20.0, 80.0, 70.0, 45.0, 45.0)]);

        var json = JsonSerializer.Serialize(model, Options);
        var result = JsonSerializer.Deserialize<DiagramModel>(json, Options)!;

        Assert.Equal(style, result.Arrows[0].Style);
        Assert.Equal(10.0, result.Arrows[0].X1);
        Assert.Equal(80.0, result.Arrows[0].X2);
        Assert.Equal(45.0, result.Arrows[0].Cx);
    }

    [Theory]
    [InlineData(PitchFormat.ElevenVElevenFull)]
    [InlineData(PitchFormat.ElevenVElevenHalf)]
    [InlineData(PitchFormat.NineVNineFull)]
    [InlineData(PitchFormat.NineVNineHalf)]
    [InlineData(PitchFormat.SevenVSevenFull)]
    [InlineData(PitchFormat.SevenVSevenHalf)]
    [InlineData(PitchFormat.Custom)]
    public void PitchFormat_SerializesAsString(PitchFormat format)
    {
        var model = new DiagramModel(format, null, null, [], [], [], [], []);

        var json = JsonSerializer.Serialize(model, Options);
        var result = JsonSerializer.Deserialize<DiagramModel>(json, Options)!;

        Assert.Equal(format, result.PitchFormat);
        Assert.Contains(format.ToString(), json);
    }

    [Fact]
    public void CustomDimensions_NullRoundTrips()
    {
        var model = new DiagramModel(PitchFormat.Custom, null, null, [], [], [], [], []);

        var json = JsonSerializer.Serialize(model, Options);
        var result = JsonSerializer.Deserialize<DiagramModel>(json, Options)!;

        Assert.Null(result.CustomWidth);
        Assert.Null(result.CustomHeight);
    }

    [Fact]
    public void CustomDimensions_ValuesRoundTrip()
    {
        var model = new DiagramModel(PitchFormat.Custom, 80.0, 50.0, [], [], [], [], []);

        var json = JsonSerializer.Serialize(model, Options);
        var result = JsonSerializer.Deserialize<DiagramModel>(json, Options)!;

        Assert.Equal(80.0, result.CustomWidth);
        Assert.Equal(50.0, result.CustomHeight);
    }

    [Fact]
    public void MultiTeamDiagram_RoundTrips()
    {
        var model = new DiagramModel(
            PitchFormat.ElevenVElevenFull, null, null,
            [
                new DiagramTeam("t1", "Red", "#e94560",
                    [new PlayerElement("9", 25.0, 60.0), new PlayerElement("10", 35.0, 50.0)]),
                new DiagramTeam("t2", "Blue", "#4169E1",
                    [new PlayerElement("GK", 50.0, 95.0)])
            ],
            [], [], [], []);

        var json = JsonSerializer.Serialize(model, Options);
        var result = JsonSerializer.Deserialize<DiagramModel>(json, Options)!;

        Assert.Equal(2, result.Teams.Count);
        Assert.Equal("t1", result.Teams[0].Id);
        Assert.Equal("Red", result.Teams[0].Name);
        Assert.Equal("#e94560", result.Teams[0].Color);
        Assert.Equal(2, result.Teams[0].Players.Count);
        Assert.Equal("GK", result.Teams[1].Players[0].Label);
    }
}
