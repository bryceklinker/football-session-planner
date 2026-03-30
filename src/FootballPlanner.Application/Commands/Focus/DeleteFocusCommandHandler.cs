using FootballPlanner.Infrastructure;
using MediatR;

namespace FootballPlanner.Application.Commands.Focus;

public class DeleteFocusCommandHandler(AppDbContext db) : IRequestHandler<DeleteFocusCommand>
{
    public async Task Handle(DeleteFocusCommand request, CancellationToken cancellationToken)
    {
        var focus = await db.Focuses.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException($"Focus {request.Id} not found.");
        db.Focuses.Remove(focus);
        await db.SaveChangesAsync(cancellationToken);
    }
}
