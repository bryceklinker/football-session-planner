using FootballPlanner.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FootballPlanner.Application.Session.Queries;

public class GetSessionByIdQueryHandler(AppDbContext db)
    : IRequestHandler<GetSessionByIdQuery, Domain.Entities.Session?>
{
    public async Task<Domain.Entities.Session?> Handle(
        GetSessionByIdQuery request, CancellationToken cancellationToken)
    {
        return await db.Sessions
            .Include(s => s.Activities.OrderBy(sa => sa.DisplayOrder))
                .ThenInclude(sa => sa.Activity)
            .Include(s => s.Activities.OrderBy(sa => sa.DisplayOrder))
                .ThenInclude(sa => sa.Phase)
            .Include(s => s.Activities.OrderBy(sa => sa.DisplayOrder))
                .ThenInclude(sa => sa.Focus)
            .Include(s => s.Activities.OrderBy(sa => sa.DisplayOrder))
                .ThenInclude(sa => sa.KeyPoints.OrderBy(kp => kp.Order))
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
    }
}
