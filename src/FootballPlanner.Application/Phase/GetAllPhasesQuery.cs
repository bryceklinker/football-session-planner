using MediatR;

namespace FootballPlanner.Application.Phase;

public record GetAllPhasesQuery : IRequest<List<Domain.Entities.Phase>>;
