using FootballPlanner.Infrastructure;
using MediatR;

namespace FootballPlanner.Application.Session;

public class DeleteSessionCommandHandler(AppDbContext db) : IRequestHandler<DeleteSessionCommand>
{
    public async Task Handle(DeleteSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await db.Sessions.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException($"Session {request.Id} not found.");
        db.Sessions.Remove(session);
        await db.SaveChangesAsync(cancellationToken);
    }
}
