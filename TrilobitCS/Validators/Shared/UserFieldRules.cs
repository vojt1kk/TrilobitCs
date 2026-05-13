using FluentValidation;

namespace TrilobitCS.Validators.Shared;

// Sdílená validační pravidla pro uživatelská pole — použij Include() ve validátorech.
public static class UserFieldRules
{
    public static IRuleBuilderOptions<T, string> NicknameRules<T>(this IRuleBuilder<T, string> rule)
        => rule.NotEmpty().MinimumLength(3).MaximumLength(20);

    public static IRuleBuilderOptions<T, string> FirstLastNameRules<T>(this IRuleBuilder<T, string> rule)
        => rule.NotEmpty().MinimumLength(3).MaximumLength(20);

    public static IRuleBuilderOptions<T, DateOnly> BirthDateRules<T>(this IRuleBuilder<T, DateOnly> rule)
        => rule.NotEmpty().LessThan(DateOnly.FromDateTime(DateTime.Today));
}
