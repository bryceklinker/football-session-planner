using MediatR;

namespace FootballPlanner.Application.Session;

public record GetAllSessionsQuery : IRequest<List<Domain.Entities.Session>>;
