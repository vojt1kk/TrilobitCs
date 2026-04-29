using FluentValidation;
using TrilobitCS.Requests;

namespace TrilobitCS.Validators;

public class UpdateOrganisationRequestValidator : AbstractValidator<UpdateOrganisationRequest>
{
    public UpdateOrganisationRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(1000);

        RuleFor(x => x.AvatarUrl)
            .MaximumLength(255);
    }
}
