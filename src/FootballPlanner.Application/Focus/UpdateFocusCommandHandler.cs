using FootballPlanner.Infrastructure;
using MediatR;

namespace FootballPlanner.Application.Focus;

public class UpdateFocusCommandHandler(AppDbContext db) : IRequestHandler<UpdateFocusCommand>
{
    public async Task Handle(UpdateFocusCommand request, CancellationToken cancellationToken)
    {
        var focus = await db.Focuses.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException($"Focus {request.Id} not found.");
        focus.Update(request.Name);
        await db.SaveChangesAsync(cancellationToken);
    }
}
