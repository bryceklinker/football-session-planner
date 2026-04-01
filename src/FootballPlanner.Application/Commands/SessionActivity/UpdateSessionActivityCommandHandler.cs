using FootballPlanner.Infrastructure;
using MediatR;

namespace FootballPlanner.Application.Commands.SessionActivity;

public class UpdateSessionActivityCommandHandler(AppDbContext db)
    : IRequestHandler<UpdateSessionActivityCommand, Domain.Entities.SessionActivity>
{
    public async Task<Domain.Entities.SessionActivity> Handle(
        UpdateSessionActivityCommand request, CancellationToken cancellationToken)
    {
        var sa = await db.SessionActivities.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException($"SessionActivity {request.Id} not found.");
        sa.Update(request.PhaseId, request.FocusId, request.Duration, request.Notes);
        await db.SaveChangesAsync(cancellationToken);
        return sa;
    }
}
