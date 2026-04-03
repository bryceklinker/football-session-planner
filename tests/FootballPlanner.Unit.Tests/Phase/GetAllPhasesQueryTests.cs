using FootballPlanner.Application.Phase;
using FootballPlanner.Unit.Tests.Infrastructure;

namespace FootballPlanner.Unit.Tests.Phase;

public class GetAllPhasesQueryTests
{
    [Fact]
    public async Task Send_ReturnsAllPhases_OrderedByOrder()
    {
        var mediator = TestServiceProvider.CreateMediator();
        await mediator.Send(new CreatePhaseCommand("Scrimmage", 3));
        await mediator.Send(new CreatePhaseCommand("Warm Up", 1));
        await mediator.Send(new CreatePhaseCommand("Main Activity", 2));

        var result = await mediator.Send(new GetAllPhasesQuery());

        Assert.Equal(3, result.Count);
        Assert.Equal("Warm Up", result[0].Name);
        Assert.Equal("Main Activity", result[1].Name);
        Assert.Equal("Scrimmage", result[2].Name);
    }

    [Fact]
    public async Task Send_ReturnsEmptyList_WhenNoPhasesExist()
    {
        var mediator = TestServiceProvider.CreateMediator();

        var result = await mediator.Send(new GetAllPhasesQuery());

        Assert.Empty(result);
    }
}
