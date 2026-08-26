using FluentValidation;
using TrilobitCS.Requests;

namespace TrilobitCS.Validators;

public class UpdateCommentRequestValidator : AbstractValidator<UpdateCommentRequest>
{
    public UpdateCommentRequestValidator()
    {
        RuleFor(r => r.Content)
            .NotEmpty()
            .MaximumLength(2000);
    }
}
