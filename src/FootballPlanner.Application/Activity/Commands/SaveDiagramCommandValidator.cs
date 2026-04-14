using FluentValidation;

namespace FootballPlanner.Application.Activity.Commands;

public class SaveDiagramCommandValidator : AbstractValidator<SaveDiagramCommand>
{
    public SaveDiagramCommandValidator()
    {
        RuleFor(x => x.ActivityId).GreaterThan(0);
    }
}
