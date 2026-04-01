using FluentValidation;

namespace FootballPlanner.Application.Commands.SessionActivity;

public class UpdateSessionActivityCommandValidator : AbstractValidator<UpdateSessionActivityCommand>
{
    public UpdateSessionActivityCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.PhaseId).GreaterThan(0);
        RuleFor(x => x.FocusId).GreaterThan(0);
        RuleFor(x => x.Duration).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(2000).When(x => x.Notes != null);
    }
}
