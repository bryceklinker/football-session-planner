using FluentValidation;

namespace FootballPlanner.Application.Session;

public class UpdateSessionCommandValidator : AbstractValidator<UpdateSessionCommand>
{
    public UpdateSessionCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(2000).When(x => x.Notes != null);
    }
}
