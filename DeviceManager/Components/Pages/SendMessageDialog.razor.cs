namespace DeviceManager.Components.Pages;

using DeviceManager.Hubs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR;

public partial class SendMessageDialog
{
    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = default!;

    [Inject]
    public MessageService MessageService { get; set; } = default!;

    [Inject]
    public IHubContext<DeviceHub> HubContext { get; set; } = default!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    private string deviceId = string.Empty;
    private string messageType = string.Empty;
    private string content = string.Empty;

    private async Task SendAsync()
    {
        if (string.IsNullOrWhiteSpace(messageType) || string.IsNullOrWhiteSpace(content))
        {
            Snackbar.Add("Message type and content are required.", Severity.Warning);
            return;
        }

        var message = new ServerMessage
        {
            DeviceId = string.IsNullOrWhiteSpace(deviceId) ? null : deviceId.Trim(),
            Direction = MessageDirection.ServerToDevice,
            MessageType = messageType.Trim(),
            Content = content.Trim(),
            Status = MessageStatus.Sent,
            CreatedAt = DateTime.UtcNow
        };

        await MessageService.AddMessageAsync(message);

        var targetDeviceId = string.IsNullOrWhiteSpace(deviceId) ? null : deviceId.Trim();
        if (!string.IsNullOrEmpty(targetDeviceId))
        {
            await HubContext.Clients.Group(HubConstants.Groups.Device(targetDeviceId))
                .SendAsync(HubConstants.ServerMethods.ReceiveMessage, messageType.Trim(), content.Trim());
        }
        else
        {
            await HubContext.Clients.Group(HubConstants.Groups.AllDevices)
                .SendAsync(HubConstants.ServerMethods.ReceiveMessage, messageType.Trim(), content.Trim());
        }

        Snackbar.Add("Message sent.", Severity.Success);
        MudDialog.Close(DialogResult.Ok(true));
    }

    private void Cancel() => MudDialog.Cancel();
}
