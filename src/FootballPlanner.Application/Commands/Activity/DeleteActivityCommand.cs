using MediatR;

namespace FootballPlanner.Application.Commands.Activity;

public record DeleteActivityCommand(int Id) : IRequest;
