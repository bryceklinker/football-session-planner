using MediatR;

namespace FootballPlanner.Application.Activity;

public record GetAllActivitiesQuery : IRequest<List<Domain.Entities.Activity>>;
