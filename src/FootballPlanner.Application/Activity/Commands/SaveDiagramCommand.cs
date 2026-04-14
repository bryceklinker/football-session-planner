using MediatR;

namespace FootballPlanner.Application.Activity.Commands;

public record SaveDiagramCommand(int ActivityId, string? DiagramJson) : IRequest;
