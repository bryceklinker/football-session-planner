using FluentValidation;

namespace FootballPlanner.Application.SessionActivity;

public class AddSessionActivityCommandValidator : AbstractValidator<AddSessionActivityCommand>
{
    public AddSessionActivityCommandValidator()
    {
        RuleFor(x => x.SessionId).GreaterThan(0);
        RuleFor(x => x.ActivityId).GreaterThan(0);
        RuleFor(x => x.PhaseId).GreaterThan(0);
        RuleFor(x => x.FocusId).GreaterThan(0);
        RuleFor(x => x.Duration).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(2000).When(x => x.Notes != null);
    }
}
