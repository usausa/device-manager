namespace DeviceManager.Server.Models.Forms;

using FluentValidation;

public sealed class MessageFormValidator : AbstractValidator<MessageForm>
{
    public MessageFormValidator()
    {
        RuleFor(static x => x.MessageType)
            .NotEmpty().WithMessage("種別を入力してください。")
            .MaximumLength(Length.MessageType).WithMessage($"種別は{Length.MessageType}文字以内で入力してください。");
        RuleFor(static x => x.Content)
            .NotEmpty().WithMessage("内容を入力してください。")
            .MaximumLength(Length.Content).WithMessage($"内容は{Length.Content}文字以内で入力してください。");
    }

    public Func<object, string, Task<IEnumerable<string>>> ValidateValue => async (model, propertyName) =>
    {
        var result = await ValidateAsync(ValidationContext<MessageForm>.CreateWithOptions((MessageForm)model, x => x.IncludeProperties(propertyName)));
        return result.IsValid ? [] : result.Errors.Select(static e => e.ErrorMessage);
    };
}
