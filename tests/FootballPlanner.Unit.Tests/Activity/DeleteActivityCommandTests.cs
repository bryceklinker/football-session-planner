using FootballPlanner.Application.Activity.Commands;
using FootballPlanner.Application.Activity.Queries;
using FootballPlanner.Unit.Tests.Infrastructure;

namespace FootballPlanner.Unit.Tests.Activity;

public class DeleteActivityCommandTests
{
    [Fact]
    public async Task Send_DeletesActivity_WhenActivityExists()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var created = await mediator.Send(
            new CreateActivityCommand("To Delete", "Desc", null, 10));

        await mediator.Send(new DeleteActivityCommand(created.Id));

        var activities = await mediator.Send(new GetAllActivitiesQuery());
        Assert.DoesNotContain(activities, a => a.Id == created.Id);
    }

    [Fact]
    public async Task Send_ThrowsKeyNotFoundException_WhenActivityNotFound()
    {
        var mediator = TestServiceProvider.CreateMediator();

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => mediator.Send(new DeleteActivityCommand(99999)));
    }
}
