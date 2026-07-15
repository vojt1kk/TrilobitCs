using FluentValidation;
using TrilobitCS.Requests;

namespace TrilobitCS.Validators;

public class ModerateUserEagleFeatherRequestValidator : AbstractValidator<ModerateUserEagleFeatherRequest>
{
    public ModerateUserEagleFeatherRequestValidator()
    {
        RuleFor(x => x.Note).MaximumLength(255).When(x => x.Note is not null);
    }
}
