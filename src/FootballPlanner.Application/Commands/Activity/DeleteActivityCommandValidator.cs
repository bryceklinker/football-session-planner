using FluentValidation;

namespace FootballPlanner.Application.Commands.Activity;

public class DeleteActivityCommandValidator : AbstractValidator<DeleteActivityCommand>
{
    public DeleteActivityCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
