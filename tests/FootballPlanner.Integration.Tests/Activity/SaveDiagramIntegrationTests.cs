using FootballPlanner.Application.Activity.Commands;
using FootballPlanner.Application.Activity.Queries;
using FootballPlanner.Integration.Tests.Infrastructure;

namespace FootballPlanner.Integration.Tests.Activity;

public class SaveDiagramIntegrationTests(TestApplication app) : IClassFixture<TestApplication>
{
    [Fact]
    public async Task SaveDiagram_PersistsToDatabaseAndRoundTrips()
    {
        var created = await app.Mediator.Send(
            new CreateActivityCommand("Pressing Drill", "High press drill", null, 15));
        var diagramJson = """{"pitchFormat":"ElevenVElevenFull","customWidth":null,"customHeight":null,"teams":[],"coaches":[],"cones":[],"goals":[],"arrows":[]}""";

        await app.Mediator.Send(new SaveDiagramCommand(created.Id, diagramJson));

        var activities = await app.Mediator.Send(new GetAllActivitiesQuery());
        var updated = activities.First(a => a.Id == created.Id);
        Assert.Equal(diagramJson, updated.DiagramJson);
    }

    [Fact]
    public async Task SaveDiagram_Null_ClearsDiagram()
    {
        var created = await app.Mediator.Send(
            new CreateActivityCommand("Rondo", "A rondo drill", null, 10));
        await app.Mediator.Send(new SaveDiagramCommand(created.Id, "{\"pitchFormat\":\"ElevenVElevenFull\"}"));

        await app.Mediator.Send(new SaveDiagramCommand(created.Id, null));

        var activities = await app.Mediator.Send(new GetAllActivitiesQuery());
        Assert.Null(activities.First(a => a.Id == created.Id).DiagramJson);
    }
}
