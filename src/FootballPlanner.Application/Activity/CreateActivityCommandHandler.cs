using FootballPlanner.Infrastructure;
using MediatR;

namespace FootballPlanner.Application.Activity;

public class CreateActivityCommandHandler(AppDbContext db)
    : IRequestHandler<CreateActivityCommand, Domain.Entities.Activity>
{
    public async Task<Domain.Entities.Activity> Handle(
        CreateActivityCommand request, CancellationToken cancellationToken)
    {
        var activity = Domain.Entities.Activity.Create(
            request.Name, request.Description, request.InspirationUrl, request.EstimatedDuration);
        db.Activities.Add(activity);
        await db.SaveChangesAsync(cancellationToken);
        return activity;
    }
}
