using FluentValidation;
using TrilobitCS.Requests;

namespace TrilobitCS.Validators;

public class UpdatePostRequestValidator : AbstractValidator<UpdatePostRequest>
{
    public UpdatePostRequestValidator()
    {
        RuleFor(r => r)
            .Must(r => r.Content is not null || r.ImageUrl is not null)
            .WithName("request")
            .WithMessage("errors.content_or_image_required");

        RuleFor(r => r.Content)
            .MaximumLength(2000);

        RuleFor(r => r.ImageUrl)
            .MaximumLength(255);
    }
}
