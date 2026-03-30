using MediatR;

namespace FootballPlanner.Application.Commands.Phase;

public record UpdatePhaseCommand(int Id, string Name, int Order) : IRequest;
