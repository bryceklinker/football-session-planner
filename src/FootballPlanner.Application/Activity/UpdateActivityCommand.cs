using MediatR;

namespace FootballPlanner.Application.Activity;

public record UpdateActivityCommand(
    int Id,
    string Name,
    string Description,
    string? InspirationUrl,
    int EstimatedDuration) : IRequest<Domain.Entities.Activity>;
