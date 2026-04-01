using MediatR;

namespace FootballPlanner.Application.Commands.Session;

public record CreateSessionCommand(
    DateTime Date,
    string Title,
    string? Notes) : IRequest<Domain.Entities.Session>;
