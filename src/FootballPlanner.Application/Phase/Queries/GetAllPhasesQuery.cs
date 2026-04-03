using MediatR;

namespace FootballPlanner.Application.Phase.Queries;

public record GetAllPhasesQuery : IRequest<List<Domain.Entities.Phase>>;
