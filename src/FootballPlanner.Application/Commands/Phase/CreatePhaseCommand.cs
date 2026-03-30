using MediatR;

namespace FootballPlanner.Application.Commands.Phase;

public record CreatePhaseCommand(string Name, int Order) : IRequest<Domain.Entities.Phase>;
