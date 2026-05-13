using FluentValidation;
using TrilobitCS.Requests;
using TrilobitCS.Validators.Shared;

namespace TrilobitCS.Validators;

// Laravel: App\Http\Requests\UpdateUserRequest::rules()
public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.Nickname).NicknameRules();
        RuleFor(x => x.FirstName).FirstLastNameRules();
        RuleFor(x => x.LastName).FirstLastNameRules();

        RuleFor(x => x.Gender).IsInEnum();

        RuleFor(x => x.BirthDate).BirthDateRules();

        RuleFor(x => x.ProfilePicture).MaximumLength(255);
    }
}
