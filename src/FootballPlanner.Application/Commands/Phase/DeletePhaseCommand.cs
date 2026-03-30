using MediatR;

namespace FootballPlanner.Application.Commands.Phase;

public record DeletePhaseCommand(int Id) : IRequest;
