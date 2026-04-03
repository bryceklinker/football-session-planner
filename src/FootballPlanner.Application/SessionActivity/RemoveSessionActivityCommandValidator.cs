using FluentValidation;

namespace FootballPlanner.Application.SessionActivity;

public class RemoveSessionActivityCommandValidator : AbstractValidator<RemoveSessionActivityCommand>
{
    public RemoveSessionActivityCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
