using MediatR;

namespace FootballPlanner.Application.Commands.Focus;

public record UpdateFocusCommand(int Id, string Name) : IRequest;
