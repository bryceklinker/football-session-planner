using FootballPlanner.Application.Activity;
using FootballPlanner.Application.Focus;
using FootballPlanner.Application.Phase;
using FootballPlanner.Application.Session;
using FootballPlanner.Application.SessionActivity;
using FootballPlanner.Unit.Tests.Infrastructure;

namespace FootballPlanner.Unit.Tests.SessionActivity;

public class UpdateSessionActivityCommandTests
{
    [Fact]
    public async Task Send_UpdatesSessionActivity_WhenCommandIsValid()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var session = await mediator.Send(new CreateSessionCommand(DateTime.UtcNow, "Test", null));
        var activity = await mediator.Send(new CreateActivityCommand("Rondo", "Desc", null, 10));
        var phase1 = await mediator.Send(new CreatePhaseCommand("Warm Up", 1));
        var phase2 = await mediator.Send(new CreatePhaseCommand("Main", 2));
        var focus = await mediator.Send(new CreateFocusCommand("Possession"));

        var sa = await mediator.Send(
            new AddSessionActivityCommand(session.Id, activity.Id, phase1.Id, focus.Id, 10, null));

        var updated = await mediator.Send(
            new UpdateSessionActivityCommand(sa.Id, phase2.Id, focus.Id, 20, "Coach notes"));

        Assert.Equal(phase2.Id, updated.PhaseId);
        Assert.Equal(20, updated.Duration);
        Assert.Equal("Coach notes", updated.Notes);
    }

    [Fact]
    public async Task Send_ThrowsKeyNotFoundException_WhenSessionActivityNotFound()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var phase = await mediator.Send(new CreatePhaseCommand("Warm Up", 1));
        var focus = await mediator.Send(new CreateFocusCommand("Possession"));

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => mediator.Send(new UpdateSessionActivityCommand(99999, phase.Id, focus.Id, 10, null)));
    }

    [Fact]
    public async Task Send_ThrowsValidationException_WhenDurationIsZero()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var session = await mediator.Send(new CreateSessionCommand(DateTime.UtcNow, "Test", null));
        var activity = await mediator.Send(new CreateActivityCommand("Rondo", "Desc", null, 10));
        var phase = await mediator.Send(new CreatePhaseCommand("Warm Up", 1));
        var focus = await mediator.Send(new CreateFocusCommand("Possession"));

        var sa = await mediator.Send(
            new AddSessionActivityCommand(session.Id, activity.Id, phase.Id, focus.Id, 15, null));

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(
            () => mediator.Send(new UpdateSessionActivityCommand(sa.Id, phase.Id, focus.Id, 0, null)));
    }
}
