using FootballPlanner.Infrastructure;
using MediatR;

namespace FootballPlanner.Application.Commands.Activity;

public class UpdateActivityCommandHandler(AppDbContext db)
    : IRequestHandler<UpdateActivityCommand, Domain.Entities.Activity>
{
    public async Task<Domain.Entities.Activity> Handle(
        UpdateActivityCommand request, CancellationToken cancellationToken)
    {
        var activity = await db.Activities.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException($"Activity {request.Id} not found.");
        activity.Update(request.Name, request.Description, request.InspirationUrl, request.EstimatedDuration);
        await db.SaveChangesAsync(cancellationToken);
        return activity;
    }
}
