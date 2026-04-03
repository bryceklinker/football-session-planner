using MediatR;

namespace FootballPlanner.Application.Session.Commands;

public record DeleteSessionCommand(int Id) : IRequest;
