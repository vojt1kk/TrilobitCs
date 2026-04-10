using FluentValidation;
using TrilobitCS.Requests;

namespace TrilobitCS.Validators;

// Laravel: App\Http\Requests\LoginRequest::rules()
public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Nickname)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(x => x.Password)
            .NotEmpty();
    }
}
