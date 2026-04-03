using FootballPlanner.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FootballPlanner.Application.SessionActivity;

public class UpdateSessionActivityKeyPointsCommandHandler(AppDbContext db)
    : IRequestHandler<UpdateSessionActivityKeyPointsCommand>
{
    public async Task Handle(
        UpdateSessionActivityKeyPointsCommand request, CancellationToken cancellationToken)
    {
        var sa = await db.SessionActivities
            .Include(x => x.KeyPoints)
            .FirstOrDefaultAsync(x => x.Id == request.SessionActivityId, cancellationToken)
            ?? throw new KeyNotFoundException($"SessionActivity {request.SessionActivityId} not found.");

        db.SessionActivityKeyPoints.RemoveRange(sa.KeyPoints);

        var order = 1;
        foreach (var text in request.KeyPoints)
        {
            db.SessionActivityKeyPoints.Add(
                Domain.Entities.SessionActivityKeyPoint.Create(sa.Id, order++, text));
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
