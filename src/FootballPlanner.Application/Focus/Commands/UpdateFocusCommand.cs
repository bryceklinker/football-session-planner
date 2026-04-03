using MediatR;

namespace FootballPlanner.Application.Focus.Commands;

public record UpdateFocusCommand(int Id, string Name) : IRequest;
