using MediatR;

namespace FootballPlanner.Application.Queries.Phase;

public record GetAllPhasesQuery : IRequest<List<Domain.Entities.Phase>>;
