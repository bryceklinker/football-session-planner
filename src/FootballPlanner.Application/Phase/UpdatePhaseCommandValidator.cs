using FluentValidation;

namespace FootballPlanner.Application.Phase;

public class UpdatePhaseCommandValidator : AbstractValidator<UpdatePhaseCommand>
{
    public UpdatePhaseCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Order).GreaterThan(0);
    }
}
