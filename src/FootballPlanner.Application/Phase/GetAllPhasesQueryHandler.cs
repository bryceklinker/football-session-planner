using FootballPlanner.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FootballPlanner.Application.Phase;

public class GetAllPhasesQueryHandler(AppDbContext db)
    : IRequestHandler<GetAllPhasesQuery, List<Domain.Entities.Phase>>
{
    public async Task<List<Domain.Entities.Phase>> Handle(
        GetAllPhasesQuery request, CancellationToken cancellationToken)
    {
        return await db.Phases
            .OrderBy(p => p.Order)
            .ToListAsync(cancellationToken);
    }
}
