using MediatR;

namespace FootballPlanner.Application.Activity.Queries;

public record GetAllActivitiesQuery : IRequest<List<Domain.Entities.Activity>>;
