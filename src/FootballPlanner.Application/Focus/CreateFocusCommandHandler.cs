using FootballPlanner.Infrastructure;
using MediatR;

namespace FootballPlanner.Application.Focus;

public class CreateFocusCommandHandler(AppDbContext db)
    : IRequestHandler<CreateFocusCommand, Domain.Entities.Focus>
{
    public async Task<Domain.Entities.Focus> Handle(
        CreateFocusCommand request, CancellationToken cancellationToken)
    {
        var focus = Domain.Entities.Focus.Create(request.Name);
        db.Focuses.Add(focus);
        await db.SaveChangesAsync(cancellationToken);
        return focus;
    }
}
