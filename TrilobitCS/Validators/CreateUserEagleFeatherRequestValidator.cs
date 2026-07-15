using FluentValidation;
using TrilobitCS.Requests;

namespace TrilobitCS.Validators;

public class CreateUserEagleFeatherRequestValidator : AbstractValidator<CreateUserEagleFeatherRequest>
{
    public CreateUserEagleFeatherRequestValidator()
    {
        RuleFor(x => x.EagleFeatherId).GreaterThan(0);
    }
}
