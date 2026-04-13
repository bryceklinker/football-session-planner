using MediatR;

namespace FootballPlanner.Application.SessionActivity.Commands;

public record ReorderItem(int SessionActivityId, int DisplayOrder);

public record ReorderSessionActivitiesCommand(
    int SessionId,
    List<ReorderItem> Items) : IRequest;
