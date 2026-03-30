using FluentValidation;

namespace FootballPlanner.Application.Commands.Focus;

public class CreateFocusCommandValidator : AbstractValidator<CreateFocusCommand>
{
    public CreateFocusCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
