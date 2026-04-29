using FluentValidation;
using TrilobitCS.Requests;

namespace TrilobitCS.Validators;

public class SendOrganisationInviteValidator : AbstractValidator<SendOrganisationInviteRequest>
{
    public SendOrganisationInviteValidator()
    {
        RuleFor(x => x.Nickname)
            .NotEmpty()
            .MaximumLength(50);
    }
}
