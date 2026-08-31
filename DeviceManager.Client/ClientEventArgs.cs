namespace DeviceManager.Client;

public sealed class ConnectionStateEventArgs : EventArgs
{
    public ConnectionState State { get; }

    public ConnectionStateEventArgs(ConnectionState state)
    {
        State = state;
    }
}

public sealed class MessageReceivedEventArgs : EventArgs
{
    public DeviceMessage Message { get; }

    public MessageReceivedEventArgs(DeviceMessage message)
    {
        Message = message;
    }
}

public sealed class ConfigReloadedEventArgs : EventArgs
{
    public IReadOnlyList<ConfigValue> Values { get; }

    public ConfigReloadedEventArgs(IReadOnlyList<ConfigValue> values)
    {
        Values = values;
    }
}
