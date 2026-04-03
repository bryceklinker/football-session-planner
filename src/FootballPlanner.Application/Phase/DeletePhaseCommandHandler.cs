using FootballPlanner.Infrastructure;
using MediatR;

namespace FootballPlanner.Application.Phase;

public class DeletePhaseCommandHandler(AppDbContext db) : IRequestHandler<DeletePhaseCommand>
{
    public async Task Handle(DeletePhaseCommand request, CancellationToken cancellationToken)
    {
        var phase = await db.Phases.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException($"Phase {request.Id} not found.");
        db.Phases.Remove(phase);
        await db.SaveChangesAsync(cancellationToken);
    }
}
