using FluentValidation;

namespace FootballPlanner.Application.Activity.Commands;

public class CreateActivityCommandValidator : AbstractValidator<CreateActivityCommand>
{
    public CreateActivityCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.InspirationUrl).MaximumLength(500).When(x => x.InspirationUrl != null);
        RuleFor(x => x.EstimatedDuration).GreaterThan(0);
    }
}
