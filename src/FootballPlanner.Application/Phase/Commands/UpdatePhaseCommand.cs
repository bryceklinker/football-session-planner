using MediatR;

namespace FootballPlanner.Application.Phase.Commands;

public record UpdatePhaseCommand(int Id, string Name, int Order) : IRequest;
