using MediatR;

namespace FootballPlanner.Application.Commands.SessionActivity;

public record UpdateSessionActivityKeyPointsCommand(
    int SessionActivityId,
    List<string> KeyPoints) : IRequest;
