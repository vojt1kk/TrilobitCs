using FluentValidation;
using TrilobitCS.Requests;

namespace TrilobitCS.Validators;

public class UpdateAnnouncementRequestValidator : AbstractValidator<UpdateAnnouncementRequest>
{
    public UpdateAnnouncementRequestValidator()
    {
        RuleFor(r => r)
            .Must(r => r.Title is not null || r.Content is not null)
            .WithName("request")
            .WithMessage("errors.title_or_content_required");

        RuleFor(r => r.Title)
            .MaximumLength(60);

        RuleFor(r => r.Content)
            .MaximumLength(300);
    }
}
