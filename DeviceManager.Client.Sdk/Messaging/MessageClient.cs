namespace DeviceManager.Client.Sdk.Messaging;

using DeviceManager.Client.Sdk.Connection;
using DeviceManager.Shared;

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

internal sealed class MessageClient : IMessageClient, IDisposable
{
    private readonly SignalRConnectionManager? signalRConnectionManager;
    private readonly GrpcConnectionManager? grpcConnectionManager;
    private readonly List<IDisposable> subscriptions = [];

    public event EventHandler<(string Type, string Content)>? MessageReceived;

    public MessageClient(
        SignalRConnectionManager? signalRConnectionManager,
        GrpcConnectionManager? grpcConnectionManager,
        ILogger logger)
    {
        this.signalRConnectionManager = signalRConnectionManager;
        this.grpcConnectionManager = grpcConnectionManager;

        if (this.signalRConnectionManager is not null)
        {
            this.signalRConnectionManager.ConnectionStateChanged += OnConnectionStateChanged;
        }

        if (this.grpcConnectionManager is not null)
        {
            this.grpcConnectionManager.MessageReceived += OnGrpcMessageReceived;
        }
    }

    public async Task SendAsync(string messageType, string content, CancellationToken cancellationToken = default)
    {
        if (grpcConnectionManager is not null)
        {
            await grpcConnectionManager.SendMessageAsync(messageType, content, cancellationToken).ConfigureAwait(false);
            return;
        }

        var hub = signalRConnectionManager?.HubConnection
            ?? throw new InvalidOperationException("Not connected to the server.");

        await hub.InvokeAsync(HubConstants.ClientMethods.SendMessage, messageType, content, cancellationToken)
            .ConfigureAwait(false);
    }

    private void OnConnectionStateChanged(object? sender, ConnectionState state)
    {
        if (state == ConnectionState.Connected)
        {
            RegisterHubHandlers();
        }
    }

    private void OnGrpcMessageReceived(object? sender, (string Type, string Content) msg)
    {
        MessageReceived?.Invoke(this, msg);
    }

    private void RegisterHubHandlers()
    {
        var hub = signalRConnectionManager?.HubConnection;
        if (hub is null) return;

        foreach (var sub in subscriptions) sub.Dispose();
        subscriptions.Clear();

        subscriptions.Add(hub.On<string, string>(HubConstants.ServerMethods.ReceiveMessage, (type, content) =>
        {
            MessageReceived?.Invoke(this, (type, content));
        }));
    }

    public void Dispose()
    {
        if (signalRConnectionManager is not null)
        {
            signalRConnectionManager.ConnectionStateChanged -= OnConnectionStateChanged;
        }

        if (grpcConnectionManager is not null)
        {
            grpcConnectionManager.MessageReceived -= OnGrpcMessageReceived;
        }

        foreach (var sub in subscriptions) sub.Dispose();
        subscriptions.Clear();
    }
}
