namespace DeviceManager.Server.Models.Forms;

using System.Text.Json;

using FluentValidation;

public sealed class FunctionFormValidator : AbstractValidator<FunctionForm>
{
    public FunctionFormValidator()
    {
        RuleFor(static x => x.Name)
            .NotEmpty().WithMessage("名前を入力してください。")
            .MaximumLength(Length.FunctionName).WithMessage($"名前は{Length.FunctionName}文字以内で入力してください。")
            .Matches("^[A-Za-z0-9._-]+$").WithMessage("名前は英数字と . _ - のみ使用できます。");
        RuleFor(static x => x.Json)
            .NotEmpty().WithMessage("JSON を入力してください。")
            .Must(BeValidJson).WithMessage("JSON の形式が正しくありません。");
    }

    private static bool BeValidJson(string json)
    {
        if (String.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public Func<object, string, Task<IEnumerable<string>>> ValidateValue => async (model, propertyName) =>
    {
        var result = await ValidateAsync(ValidationContext<FunctionForm>.CreateWithOptions((FunctionForm)model, x => x.IncludeProperties(propertyName)));
        return result.IsValid ? [] : result.Errors.Select(static e => e.ErrorMessage);
    };
}
