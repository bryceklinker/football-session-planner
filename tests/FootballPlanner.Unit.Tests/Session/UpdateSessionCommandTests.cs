using FootballPlanner.Application.Session.Commands;
using FootballPlanner.Unit.Tests.Infrastructure;

namespace FootballPlanner.Unit.Tests.Session;

public class UpdateSessionCommandTests
{
    [Fact]
    public async Task Send_UpdatesSession_WhenCommandIsValid()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var created = await mediator.Send(
            new CreateSessionCommand(new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), "Old Title", null));
        var newDate = new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc);

        var updated = await mediator.Send(
            new UpdateSessionCommand(created.Id, newDate, "New Title", "Some notes"));

        Assert.Equal("New Title", updated.Title);
        Assert.Equal(newDate, updated.Date);
        Assert.Equal("Some notes", updated.Notes);
    }

    [Fact]
    public async Task Send_ThrowsKeyNotFoundException_WhenSessionNotFound()
    {
        var mediator = TestServiceProvider.CreateMediator();
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => mediator.Send(new UpdateSessionCommand(99999, DateTime.UtcNow, "Title", null)));
    }

    [Fact]
    public async Task Send_ThrowsValidationException_WhenTitleIsEmpty()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var created = await mediator.Send(
            new CreateSessionCommand(DateTime.UtcNow, "Title", null));
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(
            () => mediator.Send(new UpdateSessionCommand(created.Id, DateTime.UtcNow, "", null)));
    }
}
