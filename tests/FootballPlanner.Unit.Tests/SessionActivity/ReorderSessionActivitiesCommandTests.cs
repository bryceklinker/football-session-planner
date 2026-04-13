using FootballPlanner.Application.Activity.Commands;
using FootballPlanner.Application.Focus.Commands;
using FootballPlanner.Application.Phase.Commands;
using FootballPlanner.Application.Session.Commands;
using FootballPlanner.Application.Session.Queries;
using FootballPlanner.Application.SessionActivity.Commands;
using FootballPlanner.Unit.Tests.Infrastructure;
using MediatR;

namespace FootballPlanner.Unit.Tests.SessionActivity;

public class ReorderSessionActivitiesCommandTests
{
    private async Task<(IMediator mediator, int sessionId, int sa1Id, int sa2Id)> SetupAsync()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var session = await mediator.Send(new CreateSessionCommand(DateTime.UtcNow, "Test Session", null));
        var activity = await mediator.Send(new CreateActivityCommand("Rondo", "Desc", null, 10));
        var phase = await mediator.Send(new CreatePhaseCommand("Warm Up", 1));
        var focus = await mediator.Send(new CreateFocusCommand("Possession"));

        var sa1 = await mediator.Send(new AddSessionActivityCommand(session.Id, activity.Id, phase.Id, focus.Id, 10, null));
        var sa2 = await mediator.Send(new AddSessionActivityCommand(session.Id, activity.Id, phase.Id, focus.Id, 15, null));

        return (mediator, session.Id, sa1.Id, sa2.Id);
    }

    [Fact]
    public async Task Send_UpdatesDisplayOrders_WhenCommandIsValid()
    {
        var (mediator, sessionId, sa1Id, sa2Id) = await SetupAsync();

        await mediator.Send(new ReorderSessionActivitiesCommand(sessionId, [
            (sa1Id, 2),
            (sa2Id, 1),
        ]));

        var session = await mediator.Send(new GetSessionByIdQuery(sessionId));
        var sa1 = session!.Activities.Single(a => a.Id == sa1Id);
        var sa2 = session.Activities.Single(a => a.Id == sa2Id);

        Assert.Equal(2, sa1.DisplayOrder);
        Assert.Equal(1, sa2.DisplayOrder);
    }

    [Fact]
    public async Task Send_ThrowsValidationException_WhenItemsIsEmpty()
    {
        var mediator = TestServiceProvider.CreateMediator();

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(
            () => mediator.Send(new ReorderSessionActivitiesCommand(1, [])));
    }

    [Fact]
    public async Task Send_ThrowsKeyNotFoundException_WhenSessionActivityNotFound()
    {
        var mediator = TestServiceProvider.CreateMediator();

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => mediator.Send(new ReorderSessionActivitiesCommand(1, [
                (99999, 1),
            ])));
    }
}
