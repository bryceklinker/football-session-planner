using FootballPlanner.Application.Phase.Commands;
using FootballPlanner.Unit.Tests.Infrastructure;

namespace FootballPlanner.Unit.Tests.Phase;

public class CreatePhaseCommandTests
{
    [Fact]
    public async Task Send_CreatesAndReturnsPhase_WhenCommandIsValid()
    {
        var mediator = TestServiceProvider.CreateMediator();

        var result = await mediator.Send(new CreatePhaseCommand("Warm Up", 1));

        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal("Warm Up", result.Name);
        Assert.Equal(1, result.Order);
    }

    [Fact]
    public async Task Send_ThrowsValidationException_WhenNameIsEmpty()
    {
        var mediator = TestServiceProvider.CreateMediator();

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(
            () => mediator.Send(new CreatePhaseCommand("", 1)));
    }
}
