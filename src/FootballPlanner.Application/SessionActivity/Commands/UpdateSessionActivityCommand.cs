using MediatR;

namespace FootballPlanner.Application.SessionActivity.Commands;

public record UpdateSessionActivityCommand(
    int Id,
    int PhaseId,
    int FocusId,
    int Duration,
    string? Notes) : IRequest<Domain.Entities.SessionActivity>;
