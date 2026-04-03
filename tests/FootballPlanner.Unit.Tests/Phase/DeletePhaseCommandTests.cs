using FootballPlanner.Application.Phase;
using FootballPlanner.Unit.Tests.Infrastructure;

namespace FootballPlanner.Unit.Tests.Phase;

public class DeletePhaseCommandTests
{
    [Fact]
    public async Task Send_RemovesPhase_WhenPhaseExists()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var created = await mediator.Send(new CreatePhaseCommand("Warm Up", 1));

        await mediator.Send(new DeletePhaseCommand(created.Id));

        var phases = await mediator.Send(new GetAllPhasesQuery());
        Assert.DoesNotContain(phases, p => p.Id == created.Id);
    }
}
