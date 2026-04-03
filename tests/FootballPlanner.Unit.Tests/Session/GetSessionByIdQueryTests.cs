using FootballPlanner.Application.Session;
using FootballPlanner.Unit.Tests.Infrastructure;

namespace FootballPlanner.Unit.Tests.Session;

public class GetSessionByIdQueryTests
{
    [Fact]
    public async Task Send_ReturnsSession_WhenSessionExists()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var created = await mediator.Send(
            new CreateSessionCommand(DateTime.UtcNow, "My Session", "Some notes"));

        var result = await mediator.Send(new GetSessionByIdQuery(created.Id));

        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
        Assert.Equal("My Session", result.Title);
        Assert.Equal("Some notes", result.Notes);
        Assert.Empty(result.Activities);
    }

    [Fact]
    public async Task Send_ReturnsNull_WhenSessionNotFound()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var result = await mediator.Send(new GetSessionByIdQuery(99999));
        Assert.Null(result);
    }
}
