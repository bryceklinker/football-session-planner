using MediatR;

namespace FootballPlanner.Application.SessionActivity;

public record UpdateSessionActivityCommand(
    int Id,
    int PhaseId,
    int FocusId,
    int Duration,
    string? Notes) : IRequest<Domain.Entities.SessionActivity>;
