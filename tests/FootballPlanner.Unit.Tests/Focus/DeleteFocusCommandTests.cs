using FootballPlanner.Application.Commands.Focus;
using FootballPlanner.Application.Queries.Focus;
using FootballPlanner.Unit.Tests.Infrastructure;

namespace FootballPlanner.Unit.Tests.Focus;

public class DeleteFocusCommandTests
{
    [Fact]
    public async Task Send_RemovesFocus_WhenFocusExists()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var created = await mediator.Send(new CreateFocusCommand("Pressing"));

        await mediator.Send(new DeleteFocusCommand(created.Id));

        var focuses = await mediator.Send(new GetAllFocusesQuery());
        Assert.DoesNotContain(focuses, f => f.Id == created.Id);
    }
}
