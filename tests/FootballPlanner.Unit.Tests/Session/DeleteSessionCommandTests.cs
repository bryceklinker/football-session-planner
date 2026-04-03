using FootballPlanner.Application.Session;
using FootballPlanner.Unit.Tests.Infrastructure;

namespace FootballPlanner.Unit.Tests.Session;

public class DeleteSessionCommandTests
{
    [Fact]
    public async Task Send_DeletesSession_WhenSessionExists()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var created = await mediator.Send(
            new CreateSessionCommand(DateTime.UtcNow, "To Delete", null));

        await mediator.Send(new DeleteSessionCommand(created.Id));

        var sessions = await mediator.Send(new GetAllSessionsQuery());
        Assert.DoesNotContain(sessions, s => s.Id == created.Id);
    }

    [Fact]
    public async Task Send_ThrowsKeyNotFoundException_WhenSessionNotFound()
    {
        var mediator = TestServiceProvider.CreateMediator();
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => mediator.Send(new DeleteSessionCommand(99999)));
    }
}
