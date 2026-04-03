using FootballPlanner.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FootballPlanner.Application.Activity.Queries;

public class GetAllActivitiesQueryHandler(AppDbContext db)
    : IRequestHandler<GetAllActivitiesQuery, List<Domain.Entities.Activity>>
{
    public async Task<List<Domain.Entities.Activity>> Handle(
        GetAllActivitiesQuery request, CancellationToken cancellationToken)
    {
        return await db.Activities
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);
    }
}
