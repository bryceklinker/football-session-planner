using MediatR;

namespace FootballPlanner.Application.Focus;

public record DeleteFocusCommand(int Id) : IRequest;
