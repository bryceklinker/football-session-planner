using FootballPlanner.Application.Commands.Focus;
using FootballPlanner.Application.Queries.Focus;
using FootballPlanner.Unit.Tests.Infrastructure;

namespace FootballPlanner.Unit.Tests.Focus;

public class GetAllFocusesQueryTests
{
    [Fact]
    public async Task Send_ReturnsAllFocuses()
    {
        var mediator = TestServiceProvider.CreateMediator();
        await mediator.Send(new CreateFocusCommand("Pressing"));
        await mediator.Send(new CreateFocusCommand("Possession"));

        var result = await mediator.Send(new GetAllFocusesQuery());

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task Send_ReturnsEmptyList_WhenNoFocusesExist()
    {
        var mediator = TestServiceProvider.CreateMediator();

        var result = await mediator.Send(new GetAllFocusesQuery());

        Assert.Empty(result);
    }
}
