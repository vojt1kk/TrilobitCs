using FluentValidation;
using TrilobitCS.Requests;

namespace TrilobitCS.Validators;

public class CreateAnnouncementRequestValidator : AbstractValidator<CreateAnnouncementRequest>
{
    public CreateAnnouncementRequestValidator()
    {
        RuleFor(r => r.Title).NotEmpty().MaximumLength(60);
        RuleFor(r => r.Content).NotEmpty().MaximumLength(300);
    }
}
