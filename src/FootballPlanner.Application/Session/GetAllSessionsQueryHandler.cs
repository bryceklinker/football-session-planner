using FootballPlanner.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FootballPlanner.Application.Session;

public class GetAllSessionsQueryHandler(AppDbContext db)
    : IRequestHandler<GetAllSessionsQuery, List<Domain.Entities.Session>>
{
    public async Task<List<Domain.Entities.Session>> Handle(
        GetAllSessionsQuery request, CancellationToken cancellationToken)
    {
        return await db.Sessions
            .OrderByDescending(s => s.Date)
            .ToListAsync(cancellationToken);
    }
}
