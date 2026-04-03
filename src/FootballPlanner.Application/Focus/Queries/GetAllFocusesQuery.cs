using MediatR;

namespace FootballPlanner.Application.Focus.Queries;

public record GetAllFocusesQuery : IRequest<List<Domain.Entities.Focus>>;
