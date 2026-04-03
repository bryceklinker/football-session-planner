using MediatR;

namespace FootballPlanner.Application.Activity.Commands;

public record DeleteActivityCommand(int Id) : IRequest;
