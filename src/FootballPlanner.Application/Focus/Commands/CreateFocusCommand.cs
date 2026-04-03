using MediatR;

namespace FootballPlanner.Application.Focus.Commands;

public record CreateFocusCommand(string Name) : IRequest<Domain.Entities.Focus>;
