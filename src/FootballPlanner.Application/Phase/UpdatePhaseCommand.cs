using MediatR;

namespace FootballPlanner.Application.Phase;

public record UpdatePhaseCommand(int Id, string Name, int Order) : IRequest;
