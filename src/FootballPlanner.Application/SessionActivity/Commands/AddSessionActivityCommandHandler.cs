using FootballPlanner.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FootballPlanner.Application.SessionActivity.Commands;

public class AddSessionActivityCommandHandler(AppDbContext db)
    : IRequestHandler<AddSessionActivityCommand, Domain.Entities.SessionActivity>
{
    public async Task<Domain.Entities.SessionActivity> Handle(
        AddSessionActivityCommand request, CancellationToken cancellationToken)
    {
        var sessionExists = await db.Sessions.AnyAsync(
            s => s.Id == request.SessionId, cancellationToken);
        if (!sessionExists)
            throw new KeyNotFoundException($"Session {request.SessionId} not found.");

        var maxOrder = await db.SessionActivities
            .Where(sa => sa.SessionId == request.SessionId)
            .MaxAsync(sa => (int?)sa.DisplayOrder, cancellationToken) ?? 0;

        var sessionActivity = Domain.Entities.SessionActivity.Create(
            request.SessionId, request.ActivityId, request.PhaseId, request.FocusId,
            request.Duration, maxOrder + 1, request.Notes);

        db.SessionActivities.Add(sessionActivity);
        await db.SaveChangesAsync(cancellationToken);
        return sessionActivity;
    }
}
