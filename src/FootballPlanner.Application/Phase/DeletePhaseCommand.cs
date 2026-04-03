using MediatR;

namespace FootballPlanner.Application.Phase;

public record DeletePhaseCommand(int Id) : IRequest;
