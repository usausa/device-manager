namespace DeviceManager.Components.Pages;

using Microsoft.AspNetCore.Components;

public partial class DeviceEditDialog
{
    [CascadingParameter] public IMudDialogInstance MudDialog { get; set; } = default!;
    [Inject] public DeviceService DeviceService { get; set; } = default!;
    [Inject] public ISnackbar Snackbar { get; set; } = default!;

    [Parameter] public string? DeviceId { get; set; }

    private bool IsEdit => !string.IsNullOrEmpty(DeviceId);

    private string formDeviceId = string.Empty;
    private string formName = string.Empty;
    private string formPlatform = string.Empty;
    private string formGroup = string.Empty;
    private string formTags = string.Empty;
    private string formNote = string.Empty;
    private bool formIsEnabled = true;

    protected override async Task OnInitializedAsync()
    {
        if (!IsEdit) return;

        var detail = await DeviceService.GetDeviceAsync(DeviceId!);
        if (detail is null) return;

        formDeviceId = detail.DeviceId;
        formName = detail.Name;
        formPlatform = detail.Platform ?? string.Empty;
        formGroup = detail.Group ?? string.Empty;
        formTags = detail.Tags is not null ? string.Join(", ", detail.Tags) : string.Empty;
        formNote = detail.Note ?? string.Empty;
        formIsEnabled = detail.IsEnabled;
    }

    private async Task SaveAsync()
    {
        if (IsEdit)
        {
            await DeviceService.UpdateDeviceAsync(DeviceId!, new DeviceUpdateRequest
            {
                Name = formName,
                Group = string.IsNullOrWhiteSpace(formGroup) ? null : formGroup,
                Tags = string.IsNullOrWhiteSpace(formTags)
                    ? null
                    : formTags.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
                Note = formNote,
                IsEnabled = formIsEnabled
            });
            Snackbar.Add($"Device '{formName}' updated.", Severity.Success);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(formDeviceId) || string.IsNullOrWhiteSpace(formName))
            {
                Snackbar.Add("Device ID and Name are required.", Severity.Warning);
                return;
            }

            await DeviceService.RegisterDeviceAsync(new DeviceRegistration
            {
                DeviceId = formDeviceId.Trim(),
                Name = formName.Trim(),
                Platform = string.IsNullOrWhiteSpace(formPlatform) ? null : formPlatform.Trim(),
                Group = string.IsNullOrWhiteSpace(formGroup) ? null : formGroup.Trim()
            });
            Snackbar.Add($"Device '{formName}' added.", Severity.Success);
        }

        MudDialog.Close(DialogResult.Ok(true));
    }

    private void Cancel() => MudDialog.Cancel();
}
