using FootballPlanner.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FootballPlanner.Application.SessionActivity.Commands;

public class ReorderSessionActivitiesCommandHandler(AppDbContext db)
    : IRequestHandler<ReorderSessionActivitiesCommand>
{
    public async Task Handle(
        ReorderSessionActivitiesCommand request, CancellationToken cancellationToken)
    {
        var sessionActivities = await db.SessionActivities
            .Where(sa => sa.SessionId == request.SessionId)
            .ToListAsync(cancellationToken);

        foreach (var (sessionActivityId, displayOrder) in request.Items)
        {
            var sa = sessionActivities.FirstOrDefault(a => a.Id == sessionActivityId)
                ?? throw new KeyNotFoundException($"SessionActivity {sessionActivityId} not found.");
            sa.UpdateDisplayOrder(displayOrder);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
