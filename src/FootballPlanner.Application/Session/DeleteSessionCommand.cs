using MediatR;

namespace FootballPlanner.Application.Session;

public record DeleteSessionCommand(int Id) : IRequest;
