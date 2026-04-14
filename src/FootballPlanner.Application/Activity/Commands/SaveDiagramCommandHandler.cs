using FootballPlanner.Infrastructure;
using MediatR;

namespace FootballPlanner.Application.Activity.Commands;

public class SaveDiagramCommandHandler(AppDbContext db) : IRequestHandler<SaveDiagramCommand>
{
    public async Task Handle(SaveDiagramCommand request, CancellationToken cancellationToken)
    {
        var activity = await db.Activities.FindAsync([request.ActivityId], cancellationToken)
            ?? throw new KeyNotFoundException($"Activity {request.ActivityId} not found.");
        activity.UpdateDiagram(request.DiagramJson);
        await db.SaveChangesAsync(cancellationToken);
    }
}
