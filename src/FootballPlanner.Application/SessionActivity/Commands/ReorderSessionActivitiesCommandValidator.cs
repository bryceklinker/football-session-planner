using FluentValidation;

namespace FootballPlanner.Application.SessionActivity.Commands;

public class ReorderSessionActivitiesCommandValidator
    : AbstractValidator<ReorderSessionActivitiesCommand>
{
    public ReorderSessionActivitiesCommandValidator()
    {
        RuleFor(x => x.SessionId).GreaterThan(0);
        RuleFor(x => x.Items).NotEmpty();
    }
}
