using FluentValidation;

namespace TrilobitCS.Validators.Shared;

// Shared validation rules for user fields — apply via Include() in validators.
public static class UserFieldRules
{
    public static IRuleBuilderOptions<T, string> NicknameRules<T>(this IRuleBuilder<T, string> rule)
        => rule.NotEmpty().MinimumLength(3).MaximumLength(20);

    public static IRuleBuilderOptions<T, string> FirstLastNameRules<T>(this IRuleBuilder<T, string> rule)
        => rule.NotEmpty().MinimumLength(3).MaximumLength(20);

    public static IRuleBuilderOptions<T, DateOnly> BirthDateRules<T>(this IRuleBuilder<T, DateOnly> rule)
        => rule.NotEmpty().LessThan(DateOnly.FromDateTime(DateTime.Today));
}
