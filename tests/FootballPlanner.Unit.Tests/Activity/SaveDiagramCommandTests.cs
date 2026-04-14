using FootballPlanner.Application.Activity.Commands;
using FootballPlanner.Application.Activity.Queries;
using FootballPlanner.Unit.Tests.Infrastructure;

namespace FootballPlanner.Unit.Tests.Activity;

public class SaveDiagramCommandTests
{
    [Fact]
    public async Task Send_SavesDiagramJson_WhenActivityExists()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var activity = await mediator.Send(
            new CreateActivityCommand("Rondo", "A rondo drill", null, 10));

        await mediator.Send(new SaveDiagramCommand(activity.Id, "{\"pitchFormat\":\"ElevenVElevenFull\"}"));

        var activities = await mediator.Send(new GetAllActivitiesQuery());
        var updated = activities.First(a => a.Id == activity.Id);
        Assert.Equal("{\"pitchFormat\":\"ElevenVElevenFull\"}", updated.DiagramJson);
    }

    [Fact]
    public async Task Send_ClearsDiagram_WhenNullPassed()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var activity = await mediator.Send(
            new CreateActivityCommand("Rondo", "A rondo drill", null, 10));
        await mediator.Send(new SaveDiagramCommand(activity.Id, "{\"pitchFormat\":\"ElevenVElevenFull\"}"));

        await mediator.Send(new SaveDiagramCommand(activity.Id, null));

        var activities = await mediator.Send(new GetAllActivitiesQuery());
        var updated = activities.First(a => a.Id == activity.Id);
        Assert.Null(updated.DiagramJson);
    }

    [Fact]
    public async Task Send_ThrowsValidationException_WhenActivityIdIsZero()
    {
        var mediator = TestServiceProvider.CreateMediator();

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(
            () => mediator.Send(new SaveDiagramCommand(0, "{}")));
    }

    [Fact]
    public async Task Send_ThrowsKeyNotFoundException_WhenActivityDoesNotExist()
    {
        var mediator = TestServiceProvider.CreateMediator();

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => mediator.Send(new SaveDiagramCommand(99999, "{}")));
    }
}
