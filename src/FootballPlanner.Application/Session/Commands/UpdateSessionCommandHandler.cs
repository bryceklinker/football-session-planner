using FootballPlanner.Infrastructure;
using MediatR;

namespace FootballPlanner.Application.Session.Commands;

public class UpdateSessionCommandHandler(AppDbContext db)
    : IRequestHandler<UpdateSessionCommand, Domain.Entities.Session>
{
    public async Task<Domain.Entities.Session> Handle(
        UpdateSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await db.Sessions.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException($"Session {request.Id} not found.");
        session.Update(request.Date, request.Title, request.Notes);
        await db.SaveChangesAsync(cancellationToken);
        return session;
    }
}
