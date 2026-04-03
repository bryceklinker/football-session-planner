using MediatR;

namespace FootballPlanner.Application.Session;

public record CreateSessionCommand(
    DateTime Date,
    string Title,
    string? Notes) : IRequest<Domain.Entities.Session>;
