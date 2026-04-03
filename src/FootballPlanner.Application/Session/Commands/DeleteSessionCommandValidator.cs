using FluentValidation;

namespace FootballPlanner.Application.Session.Commands;

public class DeleteSessionCommandValidator : AbstractValidator<DeleteSessionCommand>
{
    public DeleteSessionCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
