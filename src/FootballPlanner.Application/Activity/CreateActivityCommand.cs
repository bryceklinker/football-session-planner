using MediatR;

namespace FootballPlanner.Application.Activity;

public record CreateActivityCommand(
    string Name,
    string Description,
    string? InspirationUrl,
    int EstimatedDuration) : IRequest<Domain.Entities.Activity>;
