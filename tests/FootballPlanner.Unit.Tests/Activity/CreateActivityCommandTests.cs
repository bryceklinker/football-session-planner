using FootballPlanner.Application.Commands.Activity;
using FootballPlanner.Unit.Tests.Infrastructure;

namespace FootballPlanner.Unit.Tests.Activity;

public class CreateActivityCommandTests
{
    [Fact]
    public async Task Send_CreatesAndReturnsActivity_WhenCommandIsValid()
    {
        var mediator = TestServiceProvider.CreateMediator();

        var result = await mediator.Send(
            new CreateActivityCommand("Warm Up Rondo", "Players pass in a circle", null, 10));

        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal("Warm Up Rondo", result.Name);
        Assert.Equal("Players pass in a circle", result.Description);
        Assert.Equal(10, result.EstimatedDuration);
        Assert.Null(result.InspirationUrl);
        Assert.True(result.CreatedAt > DateTime.MinValue);
    }

    [Fact]
    public async Task Send_ThrowsValidationException_WhenNameIsEmpty()
    {
        var mediator = TestServiceProvider.CreateMediator();

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(
            () => mediator.Send(new CreateActivityCommand("", "Description", null, 10)));
    }

    [Fact]
    public async Task Send_ThrowsValidationException_WhenEstimatedDurationIsZero()
    {
        var mediator = TestServiceProvider.CreateMediator();

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(
            () => mediator.Send(new CreateActivityCommand("Name", "Description", null, 0)));
    }
}
