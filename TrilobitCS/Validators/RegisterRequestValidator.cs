using FluentValidation;
using TrilobitCS.Requests;

namespace TrilobitCS.Validators;

// Laravel: App\Http\Requests\RegisterRequest::rules()
public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Nickname)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(10);

        RuleFor(x => x.BirthDate)
            .NotEmpty()
            .LessThan(DateOnly.FromDateTime(DateTime.Today));
    }
}
