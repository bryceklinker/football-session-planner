using FootballPlanner.Application.Commands.Activity;
using FootballPlanner.Unit.Tests.Infrastructure;

namespace FootballPlanner.Unit.Tests.Activity;

public class UpdateActivityCommandTests
{
    [Fact]
    public async Task Send_UpdatesActivity_WhenCommandIsValid()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var created = await mediator.Send(
            new CreateActivityCommand("Old Name", "Old desc", null, 10));

        var updated = await mediator.Send(
            new UpdateActivityCommand(created.Id, "New Name", "New desc", "https://example.com", 20));

        Assert.Equal("New Name", updated.Name);
        Assert.Equal("New desc", updated.Description);
        Assert.Equal("https://example.com", updated.InspirationUrl);
        Assert.Equal(20, updated.EstimatedDuration);
    }

    [Fact]
    public async Task Send_ThrowsKeyNotFoundException_WhenActivityNotFound()
    {
        var mediator = TestServiceProvider.CreateMediator();

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => mediator.Send(new UpdateActivityCommand(99999, "Name", "Desc", null, 10)));
    }

    [Fact]
    public async Task Send_ThrowsValidationException_WhenNameIsEmpty()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var created = await mediator.Send(
            new CreateActivityCommand("Name", "Desc", null, 10));

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(
            () => mediator.Send(new UpdateActivityCommand(created.Id, "", "Desc", null, 10)));
    }
}
