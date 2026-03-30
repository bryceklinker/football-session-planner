using FootballPlanner.Application.Commands.Phase;
using FootballPlanner.Application.Queries.Phase;
using FootballPlanner.Integration.Tests.Infrastructure;

namespace FootballPlanner.Integration.Tests.Phase;

public class PhaseIntegrationTests(TestApplication app) : IClassFixture<TestApplication>
{
    [Fact]
    public async Task CreateAndRetrievePhase_RoundTrip()
    {
        var created = await app.Mediator.Send(new CreatePhaseCommand("Integration Warm Up", 1));

        var phases = await app.Mediator.Send(new GetAllPhasesQuery());

        Assert.Contains(phases, p => p.Id == created.Id && p.Name == "Integration Warm Up");
    }

    [Fact]
    public async Task UpdatePhase_PersistsChanges()
    {
        var created = await app.Mediator.Send(new CreatePhaseCommand("Old Name", 5));

        await app.Mediator.Send(new UpdatePhaseCommand(created.Id, "New Name", 6));

        var phases = await app.Mediator.Send(new GetAllPhasesQuery());
        var updated = phases.First(p => p.Id == created.Id);
        Assert.Equal("New Name", updated.Name);
    }

    [Fact]
    public async Task DeletePhase_RemovesFromDatabase()
    {
        var created = await app.Mediator.Send(new CreatePhaseCommand("To Delete", 9));

        await app.Mediator.Send(new DeletePhaseCommand(created.Id));

        var phases = await app.Mediator.Send(new GetAllPhasesQuery());
        Assert.DoesNotContain(phases, p => p.Id == created.Id);
    }
}
