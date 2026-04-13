using FluentValidation;

namespace FootballPlanner.Application.SessionActivity.Commands;

public class ReorderSessionActivitiesCommandValidator
    : AbstractValidator<ReorderSessionActivitiesCommand>
{
    public ReorderSessionActivitiesCommandValidator()
    {
        RuleFor(x => x.SessionId).GreaterThan(0);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).Must(item => item.SessionActivityId > 0 && item.DisplayOrder >= 0)
            .WithMessage("Each reorder item must have a valid SessionActivityId and non-negative DisplayOrder.");
    }
}
