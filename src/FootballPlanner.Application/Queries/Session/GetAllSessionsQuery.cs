using MediatR;

namespace FootballPlanner.Application.Queries.Session;

public record GetAllSessionsQuery : IRequest<List<Domain.Entities.Session>>;
