using FootballPlanner.Application.Focus.Commands;
using FootballPlanner.Application.Focus.Queries;
using FootballPlanner.Integration.Tests.Infrastructure;

namespace FootballPlanner.Integration.Tests.Focus;

public class FocusIntegrationTests(TestApplication app) : IClassFixture<TestApplication>
{
    [Fact]
    public async Task CreateAndRetrieveFocus_RoundTrip()
    {
        var created = await app.Mediator.Send(new CreateFocusCommand("Integration Pressing"));

        var focuses = await app.Mediator.Send(new GetAllFocusesQuery());

        Assert.Contains(focuses, f => f.Id == created.Id && f.Name == "Integration Pressing");
    }

    [Fact]
    public async Task UpdateFocus_PersistsChanges()
    {
        var created = await app.Mediator.Send(new CreateFocusCommand("Old Focus"));

        await app.Mediator.Send(new UpdateFocusCommand(created.Id, "New Focus"));

        var focuses = await app.Mediator.Send(new GetAllFocusesQuery());
        var updated = focuses.First(f => f.Id == created.Id);
        Assert.Equal("New Focus", updated.Name);
    }

    [Fact]
    public async Task DeleteFocus_RemovesFromDatabase()
    {
        var created = await app.Mediator.Send(new CreateFocusCommand("To Delete Focus"));

        await app.Mediator.Send(new DeleteFocusCommand(created.Id));

        var focuses = await app.Mediator.Send(new GetAllFocusesQuery());
        Assert.DoesNotContain(focuses, f => f.Id == created.Id);
    }
}
