using MediatR;

namespace FootballPlanner.Application.Session.Queries;

public record GetAllSessionsQuery : IRequest<List<Domain.Entities.Session>>;
