using FootballPlanner.Application.Commands.Activity;
using FootballPlanner.Application.Commands.Focus;
using FootballPlanner.Application.Commands.Phase;
using FootballPlanner.Application.Commands.Session;
using FootballPlanner.Application.Commands.SessionActivity;
using FootballPlanner.Unit.Tests.Infrastructure;
using MediatR;

namespace FootballPlanner.Unit.Tests.SessionActivity;

public class AddSessionActivityCommandTests
{
    private async Task<(IMediator mediator, int sessionId, int activityId, int phaseId, int focusId)> SetupAsync()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var session = await mediator.Send(new CreateSessionCommand(DateTime.UtcNow, "Test Session", null));
        var activity = await mediator.Send(new CreateActivityCommand("Rondo", "Circle passing", null, 10));
        var phase = await mediator.Send(new CreatePhaseCommand("Warm Up", 1));
        var focus = await mediator.Send(new CreateFocusCommand("Possession"));
        return (mediator, session.Id, activity.Id, phase.Id, focus.Id);
    }

    [Fact]
    public async Task Send_AddsSessionActivity_WhenCommandIsValid()
    {
        var (mediator, sessionId, activityId, phaseId, focusId) = await SetupAsync();

        var result = await mediator.Send(
            new AddSessionActivityCommand(sessionId, activityId, phaseId, focusId, 15, null));

        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal(sessionId, result.SessionId);
        Assert.Equal(activityId, result.ActivityId);
        Assert.Equal(phaseId, result.PhaseId);
        Assert.Equal(focusId, result.FocusId);
        Assert.Equal(15, result.Duration);
        Assert.Equal(1, result.DisplayOrder);
    }

    [Fact]
    public async Task Send_AssignsIncreasingDisplayOrder_WhenMultipleActivitiesAdded()
    {
        var (mediator, sessionId, activityId, phaseId, focusId) = await SetupAsync();

        var first = await mediator.Send(
            new AddSessionActivityCommand(sessionId, activityId, phaseId, focusId, 10, null));
        var second = await mediator.Send(
            new AddSessionActivityCommand(sessionId, activityId, phaseId, focusId, 20, null));

        Assert.Equal(1, first.DisplayOrder);
        Assert.Equal(2, second.DisplayOrder);
    }

    [Fact]
    public async Task Send_ThrowsKeyNotFoundException_WhenSessionNotFound()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var activity = await mediator.Send(new CreateActivityCommand("Rondo", "Desc", null, 10));
        var phase = await mediator.Send(new CreatePhaseCommand("Warm Up", 1));
        var focus = await mediator.Send(new CreateFocusCommand("Possession"));

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => mediator.Send(new AddSessionActivityCommand(99999, activity.Id, phase.Id, focus.Id, 10, null)));
    }

    [Fact]
    public async Task Send_ThrowsValidationException_WhenDurationIsZero()
    {
        var (mediator, sessionId, activityId, phaseId, focusId) = await SetupAsync();

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(
            () => mediator.Send(new AddSessionActivityCommand(sessionId, activityId, phaseId, focusId, 0, null)));
    }
}
