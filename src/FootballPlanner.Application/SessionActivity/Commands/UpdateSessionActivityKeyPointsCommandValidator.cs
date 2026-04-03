using FluentValidation;

namespace FootballPlanner.Application.SessionActivity.Commands;

public class UpdateSessionActivityKeyPointsCommandValidator : AbstractValidator<UpdateSessionActivityKeyPointsCommand>
{
    public UpdateSessionActivityKeyPointsCommandValidator()
    {
        RuleFor(x => x.SessionActivityId).GreaterThan(0);
        RuleFor(x => x.KeyPoints).NotNull();
        RuleForEach(x => x.KeyPoints).NotEmpty().MaximumLength(500);
    }
}
