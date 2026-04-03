using MediatR;

namespace FootballPlanner.Application.Session.Commands;

public record UpdateSessionCommand(
    int Id,
    DateTime Date,
    string Title,
    string? Notes) : IRequest<Domain.Entities.Session>;
