using FootballPlanner.Application.Activity.Commands;
using FootballPlanner.Application.Activity.Queries;
using FootballPlanner.Integration.Tests.Infrastructure;

namespace FootballPlanner.Integration.Tests.Activity;

public class ActivityIntegrationTests(TestApplication app) : IClassFixture<TestApplication>
{
    [Fact]
    public async Task CreateAndRetrieveActivity_RoundTrip()
    {
        var created = await app.Mediator.Send(
            new CreateActivityCommand("Integration Pressing Drill", "A drill for tests", null, 20));

        var activities = await app.Mediator.Send(new GetAllActivitiesQuery());

        Assert.Contains(activities, a => a.Id == created.Id && a.Name == "Integration Pressing Drill");
    }

    [Fact]
    public async Task UpdateActivity_PersistsChanges()
    {
        var created = await app.Mediator.Send(
            new CreateActivityCommand("Old Name", "Old description", null, 15));

        await app.Mediator.Send(
            new UpdateActivityCommand(created.Id, "New Name", "New description", "https://example.com", 25));

        var activities = await app.Mediator.Send(new GetAllActivitiesQuery());
        var updated = activities.First(a => a.Id == created.Id);
        Assert.Equal("New Name", updated.Name);
        Assert.Equal(25, updated.EstimatedDuration);
    }

    [Fact]
    public async Task DeleteActivity_RemovesFromDatabase()
    {
        var created = await app.Mediator.Send(
            new CreateActivityCommand("To Delete Activity", "Description", null, 10));

        await app.Mediator.Send(new DeleteActivityCommand(created.Id));

        var activities = await app.Mediator.Send(new GetAllActivitiesQuery());
        Assert.DoesNotContain(activities, a => a.Id == created.Id);
    }
}
