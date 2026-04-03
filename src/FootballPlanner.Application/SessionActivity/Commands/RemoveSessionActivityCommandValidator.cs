using FluentValidation;

namespace FootballPlanner.Application.SessionActivity.Commands;

public class RemoveSessionActivityCommandValidator : AbstractValidator<RemoveSessionActivityCommand>
{
    public RemoveSessionActivityCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
