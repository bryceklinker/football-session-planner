using MediatR;

namespace FootballPlanner.Application.Commands.SessionActivity;

public record RemoveSessionActivityCommand(int Id) : IRequest;
