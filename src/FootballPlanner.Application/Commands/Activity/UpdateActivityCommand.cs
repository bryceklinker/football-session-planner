using MediatR;

namespace FootballPlanner.Application.Commands.Activity;

public record UpdateActivityCommand(
    int Id,
    string Name,
    string Description,
    string? InspirationUrl,
    int EstimatedDuration) : IRequest<Domain.Entities.Activity>;
