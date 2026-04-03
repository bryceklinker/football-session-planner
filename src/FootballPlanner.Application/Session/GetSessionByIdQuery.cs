using MediatR;

namespace FootballPlanner.Application.Session;

public record GetSessionByIdQuery(int Id) : IRequest<Domain.Entities.Session?>;
