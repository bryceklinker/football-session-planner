using MediatR;

namespace FootballPlanner.Application.Focus.Commands;

public record DeleteFocusCommand(int Id) : IRequest;
