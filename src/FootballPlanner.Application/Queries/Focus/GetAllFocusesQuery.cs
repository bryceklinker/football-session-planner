using MediatR;

namespace FootballPlanner.Application.Queries.Focus;

public record GetAllFocusesQuery : IRequest<List<Domain.Entities.Focus>>;
