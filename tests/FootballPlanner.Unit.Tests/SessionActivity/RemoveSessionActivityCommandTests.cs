using FootballPlanner.Application.Activity.Commands;
using FootballPlanner.Application.Focus.Commands;
using FootballPlanner.Application.Phase.Commands;
using FootballPlanner.Application.Session.Commands;
using FootballPlanner.Application.Session.Queries;
using FootballPlanner.Application.SessionActivity.Commands;
using FootballPlanner.Unit.Tests.Infrastructure;

namespace FootballPlanner.Unit.Tests.SessionActivity;

public class RemoveSessionActivityCommandTests
{
    [Fact]
    public async Task Send_RemovesSessionActivity_WhenActivityExists()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var session = await mediator.Send(new CreateSessionCommand(DateTime.UtcNow, "Test", null));
        var activity = await mediator.Send(new CreateActivityCommand("Rondo", "Desc", null, 10));
        var phase = await mediator.Send(new CreatePhaseCommand("Warm Up", 1));
        var focus = await mediator.Send(new CreateFocusCommand("Possession"));
        var sa = await mediator.Send(
            new AddSessionActivityCommand(session.Id, activity.Id, phase.Id, focus.Id, 10, null));

        await mediator.Send(new RemoveSessionActivityCommand(sa.Id));

        var loaded = await mediator.Send(new GetSessionByIdQuery(session.Id));
        Assert.DoesNotContain(loaded!.Activities, a => a.Id == sa.Id);
    }

    [Fact]
    public async Task Send_ThrowsKeyNotFoundException_WhenSessionActivityNotFound()
    {
        var mediator = TestServiceProvider.CreateMediator();
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => mediator.Send(new RemoveSessionActivityCommand(99999)));
    }
}
