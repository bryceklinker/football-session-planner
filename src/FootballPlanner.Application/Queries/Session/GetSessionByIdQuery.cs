using MediatR;

namespace FootballPlanner.Application.Queries.Session;

public record GetSessionByIdQuery(int Id) : IRequest<Domain.Entities.Session?>;
