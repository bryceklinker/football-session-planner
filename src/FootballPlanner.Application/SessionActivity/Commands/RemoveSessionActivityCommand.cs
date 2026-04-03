using MediatR;

namespace FootballPlanner.Application.SessionActivity.Commands;

public record RemoveSessionActivityCommand(int Id) : IRequest;
