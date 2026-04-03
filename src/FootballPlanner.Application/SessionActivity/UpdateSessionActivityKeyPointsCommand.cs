using MediatR;

namespace FootballPlanner.Application.SessionActivity;

public record UpdateSessionActivityKeyPointsCommand(
    int SessionActivityId,
    List<string> KeyPoints) : IRequest;
