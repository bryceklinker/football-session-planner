using FluentValidation;

namespace FootballPlanner.Application.Focus;

public class UpdateFocusCommandValidator : AbstractValidator<UpdateFocusCommand>
{
    public UpdateFocusCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
