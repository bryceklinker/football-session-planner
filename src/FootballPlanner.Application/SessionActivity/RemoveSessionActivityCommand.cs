using MediatR;

namespace FootballPlanner.Application.SessionActivity;

public record RemoveSessionActivityCommand(int Id) : IRequest;
