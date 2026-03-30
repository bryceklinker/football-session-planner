using MediatR;

namespace FootballPlanner.Application.Commands.Focus;

public record DeleteFocusCommand(int Id) : IRequest;
