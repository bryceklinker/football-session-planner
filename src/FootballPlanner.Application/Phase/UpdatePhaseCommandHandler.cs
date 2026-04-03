using FootballPlanner.Infrastructure;
using MediatR;

namespace FootballPlanner.Application.Phase;

public class UpdatePhaseCommandHandler(AppDbContext db) : IRequestHandler<UpdatePhaseCommand>
{
    public async Task Handle(UpdatePhaseCommand request, CancellationToken cancellationToken)
    {
        var phase = await db.Phases.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException($"Phase {request.Id} not found.");
        phase.Update(request.Name, request.Order);
        await db.SaveChangesAsync(cancellationToken);
    }
}
