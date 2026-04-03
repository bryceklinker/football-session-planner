using FluentValidation;

namespace FootballPlanner.Application.Phase.Commands;

public class CreatePhaseCommandValidator : AbstractValidator<CreatePhaseCommand>
{
    public CreatePhaseCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Order).GreaterThan(0);
    }
}
