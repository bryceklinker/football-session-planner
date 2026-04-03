using FootballPlanner.Infrastructure;
using MediatR;

namespace FootballPlanner.Application.SessionActivity.Commands;

public class RemoveSessionActivityCommandHandler(AppDbContext db) : IRequestHandler<RemoveSessionActivityCommand>
{
    public async Task Handle(RemoveSessionActivityCommand request, CancellationToken cancellationToken)
    {
        var sa = await db.SessionActivities.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException($"SessionActivity {request.Id} not found.");
        db.SessionActivities.Remove(sa);
        await db.SaveChangesAsync(cancellationToken);
    }
}
