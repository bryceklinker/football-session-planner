using MediatR;

namespace FootballPlanner.Application.Session.Queries;

public record GetSessionByIdQuery(int Id) : IRequest<Domain.Entities.Session?>;
