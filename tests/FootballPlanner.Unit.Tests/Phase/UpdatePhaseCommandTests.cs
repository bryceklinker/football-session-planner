using FootballPlanner.Application.Commands.Phase;
using FootballPlanner.Application.Queries.Phase;
using FootballPlanner.Unit.Tests.Infrastructure;

namespace FootballPlanner.Unit.Tests.Phase;

public class UpdatePhaseCommandTests
{
    [Fact]
    public async Task Send_UpdatesPhase_WhenCommandIsValid()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var created = await mediator.Send(new CreatePhaseCommand("Warm Up", 1));

        await mediator.Send(new UpdatePhaseCommand(created.Id, "Activation", 2));

        var phases = await mediator.Send(new GetAllPhasesQuery());
        var updated = phases.Single(p => p.Id == created.Id);
        Assert.Equal("Activation", updated.Name);
        Assert.Equal(2, updated.Order);
    }

    [Fact]
    public async Task Send_ThrowsValidationException_WhenNameIsEmpty()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var created = await mediator.Send(new CreatePhaseCommand("Warm Up", 1));

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(
            () => mediator.Send(new UpdatePhaseCommand(created.Id, "", 1)));
    }
}
