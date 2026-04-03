using MediatR;

namespace FootballPlanner.Application.Phase.Commands;

public record CreatePhaseCommand(string Name, int Order) : IRequest<Domain.Entities.Phase>;
