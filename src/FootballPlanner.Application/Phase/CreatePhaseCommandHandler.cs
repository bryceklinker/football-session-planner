using FootballPlanner.Infrastructure;
using MediatR;

namespace FootballPlanner.Application.Phase;

public class CreatePhaseCommandHandler(AppDbContext db)
    : IRequestHandler<CreatePhaseCommand, Domain.Entities.Phase>
{
    public async Task<Domain.Entities.Phase> Handle(
        CreatePhaseCommand request, CancellationToken cancellationToken)
    {
        var phase = Domain.Entities.Phase.Create(request.Name, request.Order);
        db.Phases.Add(phase);
        await db.SaveChangesAsync(cancellationToken);
        return phase;
    }
}
