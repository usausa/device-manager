namespace DeviceManager.Client.Sdk.Messaging;

public interface IMessageClient
{
    event EventHandler<(string Type, string Content)>? MessageReceived;
    Task SendAsync(string messageType, string content, CancellationToken cancellationToken = default);
}
