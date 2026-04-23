using FluentValidation;
using TrilobitCS.Requests;

namespace TrilobitCS.Validators;

// Laravel: App\Http\Requests\UpdateUserRequest::rules()
public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.Nickname)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(20);

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(20);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(20);

        RuleFor(x => x.Gender)
            .IsInEnum();

        RuleFor(x => x.BirthDate)
            .NotEmpty()
            .LessThan(DateOnly.FromDateTime(DateTime.Today));

        RuleFor(x => x.ProfilePicture)
            .MaximumLength(255);
    }
}
