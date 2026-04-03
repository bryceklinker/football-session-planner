using MediatR;

namespace FootballPlanner.Application.Session.Commands;

public record CreateSessionCommand(
    DateTime Date,
    string Title,
    string? Notes) : IRequest<Domain.Entities.Session>;
