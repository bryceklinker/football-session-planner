using FootballPlanner.Application.Session;
using FootballPlanner.Unit.Tests.Infrastructure;

namespace FootballPlanner.Unit.Tests.Session;

public class GetAllSessionsQueryTests
{
    [Fact]
    public async Task Send_ReturnsEmptyList_WhenNoSessionsExist()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var sessions = await mediator.Send(new GetAllSessionsQuery());
        Assert.NotNull(sessions);
        Assert.Empty(sessions);
    }

    [Fact]
    public async Task Send_ReturnsSessionsOrderedByDateDescending()
    {
        var mediator = TestServiceProvider.CreateMediator();
        await mediator.Send(new CreateSessionCommand(
            new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), "March Session", null));
        await mediator.Send(new CreateSessionCommand(
            new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), "April Session", null));

        var sessions = await mediator.Send(new GetAllSessionsQuery());

        Assert.Equal("April Session", sessions[0].Title);
        Assert.Equal("March Session", sessions[1].Title);
    }
}
