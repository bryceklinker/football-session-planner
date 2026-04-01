using FluentValidation;

namespace FootballPlanner.Application.Commands.Session;

public class DeleteSessionCommandValidator : AbstractValidator<DeleteSessionCommand>
{
    public DeleteSessionCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
