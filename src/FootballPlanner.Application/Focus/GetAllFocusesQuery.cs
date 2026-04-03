using MediatR;

namespace FootballPlanner.Application.Focus;

public record GetAllFocusesQuery : IRequest<List<Domain.Entities.Focus>>;
