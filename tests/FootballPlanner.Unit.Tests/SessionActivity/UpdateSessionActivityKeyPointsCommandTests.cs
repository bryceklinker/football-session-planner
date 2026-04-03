using FootballPlanner.Application.Activity;
using FootballPlanner.Application.Focus;
using FootballPlanner.Application.Phase;
using FootballPlanner.Application.Session;
using FootballPlanner.Application.SessionActivity;
using FootballPlanner.Unit.Tests.Infrastructure;

namespace FootballPlanner.Unit.Tests.SessionActivity;

public class UpdateSessionActivityKeyPointsCommandTests
{
    [Fact]
    public async Task Send_ReplacesKeyPoints_WhenCommandIsValid()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var session = await mediator.Send(new CreateSessionCommand(DateTime.UtcNow, "Test", null));
        var activity = await mediator.Send(new CreateActivityCommand("Rondo", "Desc", null, 10));
        var phase = await mediator.Send(new CreatePhaseCommand("Warm Up", 1));
        var focus = await mediator.Send(new CreateFocusCommand("Possession"));
        var sa = await mediator.Send(
            new AddSessionActivityCommand(session.Id, activity.Id, phase.Id, focus.Id, 10, null));

        await mediator.Send(new UpdateSessionActivityKeyPointsCommand(
            sa.Id, ["Keep possession", "Press immediately on turnover"]));

        var loaded = await mediator.Send(new GetSessionByIdQuery(session.Id));
        var keyPoints = loaded!.Activities.First(a => a.Id == sa.Id).KeyPoints;
        Assert.Equal(2, keyPoints.Count);
        Assert.Equal("Keep possession", keyPoints[0].Text);
        Assert.Equal("Press immediately on turnover", keyPoints[1].Text);
    }

    [Fact]
    public async Task Send_ClearsKeyPoints_WhenEmptyListProvided()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var session = await mediator.Send(new CreateSessionCommand(DateTime.UtcNow, "Test", null));
        var activity = await mediator.Send(new CreateActivityCommand("Rondo", "Desc", null, 10));
        var phase = await mediator.Send(new CreatePhaseCommand("Warm Up", 1));
        var focus = await mediator.Send(new CreateFocusCommand("Possession"));
        var sa = await mediator.Send(
            new AddSessionActivityCommand(session.Id, activity.Id, phase.Id, focus.Id, 10, null));
        await mediator.Send(new UpdateSessionActivityKeyPointsCommand(sa.Id, ["Initial point"]));

        await mediator.Send(new UpdateSessionActivityKeyPointsCommand(sa.Id, []));

        var loaded = await mediator.Send(new GetSessionByIdQuery(session.Id));
        Assert.Empty(loaded!.Activities.First(a => a.Id == sa.Id).KeyPoints);
    }

    [Fact]
    public async Task Send_ThrowsKeyNotFoundException_WhenSessionActivityNotFound()
    {
        var mediator = TestServiceProvider.CreateMediator();
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => mediator.Send(new UpdateSessionActivityKeyPointsCommand(99999, ["Point"])));
    }
}
