using MediatR;

namespace FootballPlanner.Application.Activity;

public record DeleteActivityCommand(int Id) : IRequest;
