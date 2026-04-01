using FluentValidation;

namespace FootballPlanner.Application.Commands.SessionActivity;

public class UpdateSessionActivityKeyPointsCommandValidator : AbstractValidator<UpdateSessionActivityKeyPointsCommand>
{
    public UpdateSessionActivityKeyPointsCommandValidator()
    {
        RuleFor(x => x.SessionActivityId).GreaterThan(0);
        RuleForEach(x => x.KeyPoints).NotEmpty().MaximumLength(500);
    }
}
