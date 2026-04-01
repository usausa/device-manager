using DeviceManager.Client.Sdk.Connection;
using DeviceManager.Shared;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace DeviceManager.Client.Sdk.Messaging;

internal sealed class MessageClient : IMessageClient, IDisposable
{
    private readonly SignalRConnectionManager? _signalRConnectionManager;
    private readonly GrpcConnectionManager? _grpcConnectionManager;
    private readonly ILogger _logger;
    private readonly List<IDisposable> _subscriptions = [];

    public event EventHandler<(string Type, string Content)>? MessageReceived;

    public MessageClient(
        SignalRConnectionManager? signalRConnectionManager,
        GrpcConnectionManager? grpcConnectionManager,
        ILogger logger)
    {
        _signalRConnectionManager = signalRConnectionManager;
        _grpcConnectionManager = grpcConnectionManager;
        _logger = logger;

        if (_signalRConnectionManager is not null)
        {
            _signalRConnectionManager.ConnectionStateChanged += OnConnectionStateChanged;
        }

        if (_grpcConnectionManager is not null)
        {
            _grpcConnectionManager.MessageReceived += OnGrpcMessageReceived;
        }
    }

    public async Task SendAsync(string messageType, string content, CancellationToken cancellationToken = default)
    {
        if (_grpcConnectionManager is not null)
        {
            await _grpcConnectionManager.SendMessageAsync(messageType, content, cancellationToken).ConfigureAwait(false);
            return;
        }

        var hub = _signalRConnectionManager?.HubConnection
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
        var hub = _signalRConnectionManager?.HubConnection;
        if (hub is null) return;

        foreach (var sub in _subscriptions) sub.Dispose();
        _subscriptions.Clear();

        _subscriptions.Add(hub.On<string, string>(HubConstants.ServerMethods.ReceiveMessage, (type, content) =>
        {
            MessageReceived?.Invoke(this, (type, content));
        }));
    }

    public void Dispose()
    {
        if (_signalRConnectionManager is not null)
        {
            _signalRConnectionManager.ConnectionStateChanged -= OnConnectionStateChanged;
        }

        if (_grpcConnectionManager is not null)
        {
            _grpcConnectionManager.MessageReceived -= OnGrpcMessageReceived;
        }

        foreach (var sub in _subscriptions) sub.Dispose();
        _subscriptions.Clear();
    }
}
