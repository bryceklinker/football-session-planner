using MediatR;

namespace FootballPlanner.Application.Phase;

public record CreatePhaseCommand(string Name, int Order) : IRequest<Domain.Entities.Phase>;
