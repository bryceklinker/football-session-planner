using MediatR;

namespace FootballPlanner.Application.Focus;

public record CreateFocusCommand(string Name) : IRequest<Domain.Entities.Focus>;
