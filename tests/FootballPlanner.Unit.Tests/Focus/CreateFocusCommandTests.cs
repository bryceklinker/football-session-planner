using FootballPlanner.Application.Focus;
using FootballPlanner.Unit.Tests.Infrastructure;

namespace FootballPlanner.Unit.Tests.Focus;

public class CreateFocusCommandTests
{
    [Fact]
    public async Task Send_CreatesAndReturnsFocus_WhenCommandIsValid()
    {
        var mediator = TestServiceProvider.CreateMediator();

        var result = await mediator.Send(new CreateFocusCommand("Pressing"));

        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal("Pressing", result.Name);
    }

    [Fact]
    public async Task Send_ThrowsValidationException_WhenNameIsEmpty()
    {
        var mediator = TestServiceProvider.CreateMediator();

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(
            () => mediator.Send(new CreateFocusCommand("")));
    }
}
