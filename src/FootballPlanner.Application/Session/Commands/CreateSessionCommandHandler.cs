using FootballPlanner.Infrastructure;
using MediatR;

namespace FootballPlanner.Application.Session.Commands;

public class CreateSessionCommandHandler(AppDbContext db)
    : IRequestHandler<CreateSessionCommand, Domain.Entities.Session>
{
    public async Task<Domain.Entities.Session> Handle(
        CreateSessionCommand request, CancellationToken cancellationToken)
    {
        var session = Domain.Entities.Session.Create(request.Date, request.Title, request.Notes);
        db.Sessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);
        return session;
    }
}
