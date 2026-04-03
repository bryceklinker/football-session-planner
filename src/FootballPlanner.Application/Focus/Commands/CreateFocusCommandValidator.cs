using FluentValidation;

namespace FootballPlanner.Application.Focus.Commands;

public class CreateFocusCommandValidator : AbstractValidator<CreateFocusCommand>
{
    public CreateFocusCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
