using MediatR;

namespace FootballPlanner.Application.Commands.SessionActivity;

public record AddSessionActivityCommand(
    int SessionId,
    int ActivityId,
    int PhaseId,
    int FocusId,
    int Duration,
    string? Notes) : IRequest<Domain.Entities.SessionActivity>;
