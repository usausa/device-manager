namespace DeviceManager.Server.Models.Forms;

using FluentValidation;

public sealed class DeviceFormValidator : AbstractValidator<DeviceForm>
{
    public DeviceFormValidator()
    {
        RuleFor(static x => x.DeviceId)
            .NotEmpty().WithMessage("端末IDを入力してください。")
            .MaximumLength(Length.DeviceId).WithMessage($"端末IDは{Length.DeviceId}文字以内で入力してください。")
            .Matches("^[A-Za-z0-9._-]+$").WithMessage("端末IDは英数字と . _ - のみ使用できます。");
        RuleFor(static x => x.Name)
            .NotEmpty().WithMessage("名称を入力してください。")
            .MaximumLength(Length.Name).WithMessage($"名称は{Length.Name}文字以内で入力してください。");
        RuleFor(static x => x.GroupName)
            .MaximumLength(Length.GroupName).WithMessage($"グループは{Length.GroupName}文字以内で入力してください。");
        RuleFor(static x => x.Note)
            .MaximumLength(Length.Note).WithMessage($"備考は{Length.Note}文字以内で入力してください。");
    }

    public Func<object, string, Task<IEnumerable<string>>> ValidateValue => async (model, propertyName) =>
    {
        var result = await ValidateAsync(ValidationContext<DeviceForm>.CreateWithOptions((DeviceForm)model, x => x.IncludeProperties(propertyName)));
        return result.IsValid ? [] : result.Errors.Select(static e => e.ErrorMessage);
    };
}
