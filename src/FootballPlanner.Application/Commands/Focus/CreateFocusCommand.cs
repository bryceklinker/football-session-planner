using MediatR;

namespace FootballPlanner.Application.Commands.Focus;

public record CreateFocusCommand(string Name) : IRequest<Domain.Entities.Focus>;
