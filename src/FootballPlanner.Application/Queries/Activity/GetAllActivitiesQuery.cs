using MediatR;

namespace FootballPlanner.Application.Queries.Activity;

public record GetAllActivitiesQuery : IRequest<List<Domain.Entities.Activity>>;
