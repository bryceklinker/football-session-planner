using FootballPlanner.Application.Session;
using FootballPlanner.Unit.Tests.Infrastructure;

namespace FootballPlanner.Unit.Tests.Session;

public class CreateSessionCommandTests
{
    [Fact]
    public async Task Send_CreatesAndReturnsSession_WhenCommandIsValid()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var date = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);

        var result = await mediator.Send(new CreateSessionCommand(date, "Tuesday U10s", null));

        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal(date, result.Date);
        Assert.Equal("Tuesday U10s", result.Title);
        Assert.Null(result.Notes);
        Assert.True(result.CreatedAt > DateTime.MinValue);
    }

    [Fact]
    public async Task Send_ThrowsValidationException_WhenTitleIsEmpty()
    {
        var mediator = TestServiceProvider.CreateMediator();
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(
            () => mediator.Send(new CreateSessionCommand(DateTime.UtcNow, "", null)));
    }
}
