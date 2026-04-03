using FootballPlanner.Application.Focus;
using FootballPlanner.Unit.Tests.Infrastructure;

namespace FootballPlanner.Unit.Tests.Focus;

public class UpdateFocusCommandTests
{
    [Fact]
    public async Task Send_UpdatesFocus_WhenCommandIsValid()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var created = await mediator.Send(new CreateFocusCommand("Pressing"));

        await mediator.Send(new UpdateFocusCommand(created.Id, "Counter Press"));

        var focuses = await mediator.Send(new GetAllFocusesQuery());
        var updated = focuses.Single(f => f.Id == created.Id);
        Assert.Equal("Counter Press", updated.Name);
    }
}
