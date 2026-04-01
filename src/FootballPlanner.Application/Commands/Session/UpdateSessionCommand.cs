using MediatR;

namespace FootballPlanner.Application.Commands.Session;

public record UpdateSessionCommand(
    int Id,
    DateTime Date,
    string Title,
    string? Notes) : IRequest<Domain.Entities.Session>;
