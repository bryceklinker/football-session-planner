using MediatR;

namespace FootballPlanner.Application.SessionActivity.Commands;

public record ReorderSessionActivitiesCommand(
    int SessionId,
    IReadOnlyList<(int SessionActivityId, int DisplayOrder)> Items) : IRequest;
