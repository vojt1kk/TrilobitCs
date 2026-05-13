using FluentValidation;
using TrilobitCS.Requests;
using TrilobitCS.Validators.Shared;

namespace TrilobitCS.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Nickname).NicknameRules();
        RuleFor(x => x.FirstName).FirstLastNameRules();
        RuleFor(x => x.LastName).FirstLastNameRules();

        RuleFor(x => x.Email)
            .NotEmpty()
            .MinimumLength(6)
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(10);

        RuleFor(x => x.PasswordConfirm)
            .Equal(x => x.Password)
            .WithMessage("'Password Confirm' must match 'Password'.");

        RuleFor(x => x.BirthDate).BirthDateRules();
    }
}
