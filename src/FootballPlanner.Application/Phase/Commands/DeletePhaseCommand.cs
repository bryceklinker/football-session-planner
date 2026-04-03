using MediatR;

namespace FootballPlanner.Application.Phase.Commands;

public record DeletePhaseCommand(int Id) : IRequest;
