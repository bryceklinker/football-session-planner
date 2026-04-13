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

        foreach (var item in request.Items)
        {
            var sa = sessionActivities.FirstOrDefault(a => a.Id == item.SessionActivityId);
            sa?.UpdateDisplayOrder(item.DisplayOrder);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
