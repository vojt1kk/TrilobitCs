using FluentValidation;
using TrilobitCS.Requests;

namespace TrilobitCS.Validators;

public class CreateCommentRequestValidator : AbstractValidator<CreateCommentRequest>
{
    public CreateCommentRequestValidator()
    {
        RuleFor(r => r.Content)
            .NotEmpty()
            .MaximumLength(2000);
    }
}
