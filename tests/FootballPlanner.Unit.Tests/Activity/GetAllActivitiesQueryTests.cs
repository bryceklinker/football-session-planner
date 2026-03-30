using FootballPlanner.Application.Commands.Activity;
using FootballPlanner.Application.Queries.Activity;
using FootballPlanner.Unit.Tests.Infrastructure;

namespace FootballPlanner.Unit.Tests.Activity;

public class GetAllActivitiesQueryTests
{
    [Fact]
    public async Task Send_ReturnsEmptyList_WhenNoActivitiesExist()
    {
        var mediator = TestServiceProvider.CreateMediator();

        var activities = await mediator.Send(new GetAllActivitiesQuery());

        Assert.NotNull(activities);
        Assert.Empty(activities);
    }

    [Fact]
    public async Task Send_ReturnsActivitiesOrderedByName()
    {
        var mediator = TestServiceProvider.CreateMediator();
        await mediator.Send(new CreateActivityCommand("Zonal Marking", "Desc", null, 15));
        await mediator.Send(new CreateActivityCommand("Attacking Patterns", "Desc", null, 20));

        var activities = await mediator.Send(new GetAllActivitiesQuery());

        var names = activities.Select(a => a.Name).ToList();
        Assert.Equal(names.OrderBy(n => n).ToList(), names);
    }
}
