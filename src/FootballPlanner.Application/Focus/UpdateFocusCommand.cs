using MediatR;

namespace FootballPlanner.Application.Focus;

public record UpdateFocusCommand(int Id, string Name) : IRequest;
