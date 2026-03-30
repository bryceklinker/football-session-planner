using FootballPlanner.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FootballPlanner.Application.Queries.Focus;

public class GetAllFocusesQueryHandler(AppDbContext db) : IRequestHandler<GetAllFocusesQuery, List<Domain.Entities.Focus>>
{
    public async Task<List<Domain.Entities.Focus>> Handle(GetAllFocusesQuery request, CancellationToken cancellationToken)
    {
        return await db.Focuses
            .OrderBy(f => f.Name)
            .ToListAsync(cancellationToken);
    }
}
