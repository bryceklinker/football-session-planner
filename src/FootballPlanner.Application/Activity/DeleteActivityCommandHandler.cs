using FootballPlanner.Infrastructure;
using MediatR;

namespace FootballPlanner.Application.Activity;

public class DeleteActivityCommandHandler(AppDbContext db) : IRequestHandler<DeleteActivityCommand>
{
    public async Task Handle(DeleteActivityCommand request, CancellationToken cancellationToken)
    {
        var activity = await db.Activities.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException($"Activity {request.Id} not found.");
        db.Activities.Remove(activity);
        await db.SaveChangesAsync(cancellationToken);
    }
}
