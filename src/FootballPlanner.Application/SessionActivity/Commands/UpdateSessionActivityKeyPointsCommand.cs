using MediatR;

namespace FootballPlanner.Application.SessionActivity.Commands;

public record UpdateSessionActivityKeyPointsCommand(
    int SessionActivityId,
    List<string> KeyPoints) : IRequest;
