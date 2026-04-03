using FluentValidation;

namespace FootballPlanner.Application.Phase;

public class CreatePhaseCommandValidator : AbstractValidator<CreatePhaseCommand>
{
    public CreatePhaseCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Order).GreaterThan(0);
    }
}
