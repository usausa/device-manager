namespace DeviceManager.Server.Models.Forms;

using FluentValidation;

public sealed class ConfigFormValidator : AbstractValidator<ConfigForm>
{
    public ConfigFormValidator()
    {
        RuleFor(static x => x.Key)
            .NotEmpty().WithMessage("キーを入力してください。")
            .MaximumLength(Length.Key).WithMessage($"キーは{Length.Key}文字以内で入力してください。")
            .Matches("^[A-Za-z0-9._:-]+$").WithMessage("キーは英数字と . _ : - のみ使用できます。");
        RuleFor(static x => x.Value)
            .NotEmpty().WithMessage("値を入力してください。")
            .MaximumLength(Length.Value).WithMessage($"値は{Length.Value}文字以内で入力してください。");
        RuleFor(static x => x.Description)
            .MaximumLength(Length.Description).WithMessage($"説明は{Length.Description}文字以内で入力してください。");
    }

    public Func<object, string, Task<IEnumerable<string>>> ValidateValue => async (model, propertyName) =>
    {
        var result = await ValidateAsync(ValidationContext<ConfigForm>.CreateWithOptions((ConfigForm)model, x => x.IncludeProperties(propertyName)));
        return result.IsValid ? [] : result.Errors.Select(static e => e.ErrorMessage);
    };
}
