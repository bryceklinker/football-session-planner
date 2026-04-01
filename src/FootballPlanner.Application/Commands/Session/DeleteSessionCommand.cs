using MediatR;

namespace FootballPlanner.Application.Commands.Session;

public record DeleteSessionCommand(int Id) : IRequest;
