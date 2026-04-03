using FootballPlanner.Application.Activity;
using FootballPlanner.Application.Focus;
using FootballPlanner.Application.Phase;
using FootballPlanner.Application.Session;
using FootballPlanner.Application.SessionActivity;
using FootballPlanner.Integration.Tests.Infrastructure;

namespace FootballPlanner.Integration.Tests.Session;

public class SessionIntegrationTests(TestApplication app) : IClassFixture<TestApplication>
{
    [Fact]
    public async Task CreateAndRetrieveSession_RoundTrip()
    {
        var date = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var created = await app.Mediator.Send(new CreateSessionCommand(date, "Integration Session", "Notes here"));

        var sessions = await app.Mediator.Send(new GetAllSessionsQuery());

        Assert.Contains(sessions, s => s.Id == created.Id && s.Title == "Integration Session");
    }

    [Fact]
    public async Task UpdateSession_PersistsChanges()
    {
        var created = await app.Mediator.Send(
            new CreateSessionCommand(DateTime.UtcNow, "Old Title", null));
        var newDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        await app.Mediator.Send(new UpdateSessionCommand(created.Id, newDate, "New Title", "Updated notes"));

        var sessions = await app.Mediator.Send(new GetAllSessionsQuery());
        var updated = sessions.First(s => s.Id == created.Id);
        Assert.Equal("New Title", updated.Title);
    }

    [Fact]
    public async Task DeleteSession_RemovesFromDatabase()
    {
        var created = await app.Mediator.Send(
            new CreateSessionCommand(DateTime.UtcNow, "To Delete", null));

        await app.Mediator.Send(new DeleteSessionCommand(created.Id));

        var sessions = await app.Mediator.Send(new GetAllSessionsQuery());
        Assert.DoesNotContain(sessions, s => s.Id == created.Id);
    }

    [Fact]
    public async Task AddSessionActivity_AndLoadWithGetById_PersistsRelatedData()
    {
        var session = await app.Mediator.Send(
            new CreateSessionCommand(DateTime.UtcNow, "Activity Test Session", null));
        var activity = await app.Mediator.Send(
            new CreateActivityCommand("Pressing Drill", "High press drill", null, 15));
        var phase = await app.Mediator.Send(new CreatePhaseCommand("Main", 2));
        var focus = await app.Mediator.Send(new CreateFocusCommand("Pressing"));

        await app.Mediator.Send(new AddSessionActivityCommand(
            session.Id, activity.Id, phase.Id, focus.Id, 20, "Focus on trigger"));

        var loaded = await app.Mediator.Send(new GetSessionByIdQuery(session.Id));
        Assert.NotNull(loaded);
        Assert.Single(loaded.Activities);
        var sa = loaded.Activities[0];
        Assert.Equal(activity.Id, sa.ActivityId);
        Assert.Equal("Pressing Drill", sa.Activity.Name);
        Assert.Equal("Main", sa.Phase.Name);
        Assert.Equal("Pressing", sa.Focus.Name);
        Assert.Equal(20, sa.Duration);
        Assert.Equal(1, sa.DisplayOrder);
    }

    [Fact]
    public async Task UpdateSessionActivityKeyPoints_PersistsKeyPoints()
    {
        var session = await app.Mediator.Send(
            new CreateSessionCommand(DateTime.UtcNow, "KP Test Session", null));
        var activity = await app.Mediator.Send(
            new CreateActivityCommand("Rondo", "Circle passing", null, 10));
        var phase = await app.Mediator.Send(new CreatePhaseCommand("Warm Up", 1));
        var focus = await app.Mediator.Send(new CreateFocusCommand("Possession"));
        var sa = await app.Mediator.Send(
            new AddSessionActivityCommand(session.Id, activity.Id, phase.Id, focus.Id, 10, null));

        await app.Mediator.Send(new UpdateSessionActivityKeyPointsCommand(
            sa.Id, ["Stay compact", "Quick release"]));

        var loaded = await app.Mediator.Send(new GetSessionByIdQuery(session.Id));
        var keyPoints = loaded!.Activities.First(a => a.Id == sa.Id).KeyPoints;
        Assert.Equal(2, keyPoints.Count);
        Assert.Equal("Stay compact", keyPoints[0].Text);
        Assert.Equal("Quick release", keyPoints[1].Text);
    }
}
