using System.Windows;
using System.Windows.Media;
using DeviceManager.Client.Sdk;
using DeviceManager.Shared.Models;
using Microsoft.Extensions.Logging;

namespace DeviceManager.TestClient;

public partial class MainWindow : Window
{
    private DeviceManagerClient? _client;
    private TestDeviceInfoProvider? _deviceInfoProvider;
    private TestStatusProvider? _statusProvider;
    private TestCommandHandler? _commandHandler;
    private ILoggerFactory? _loggerFactory;

    public MainWindow()
    {
        InitializeComponent();

        var machineName = Environment.MachineName.ToLower();
        DeviceIdBox.Text = $"test-{machineName}";
        DeviceNameBox.Text = $"TestClient-{Environment.MachineName}";
    }

    private void Log(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}";
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(() => AppendLog(line));
        }
        else
        {
            AppendLog(line);
        }
    }

    private void AppendLog(string line)
    {
        LogBox.AppendText(line);
        LogBox.ScrollToEnd();
    }

    private void UpdateConnectionState(ConnectionState state)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(() => UpdateConnectionState(state));
            return;
        }

        var (color, tooltip) = state switch
        {
            ConnectionState.Connected => (Brushes.LimeGreen, "Connected"),
            ConnectionState.Connecting => (Brushes.Orange, "Connecting..."),
            ConnectionState.Reconnecting => (Brushes.Yellow, "Reconnecting..."),
            _ => (Brushes.Gray, "Disconnected")
        };

        StatusIndicator.Background = color;
        StatusIndicator.ToolTip = tooltip;

        var isConnected = state == ConnectionState.Connected;
        ConnectBtn.IsEnabled = state == ConnectionState.Disconnected;
        DisconnectBtn.IsEnabled = isConnected;
        SendStatusBtn.IsEnabled = isConnected;
        StartAutoReportBtn.IsEnabled = isConnected;
        SendMsgBtn.IsEnabled = isConnected;
        RefreshConfigBtn.IsEnabled = isConnected;
        DataGetBtn.IsEnabled = isConnected;
        DataSetBtn.IsEnabled = isConnected;
        DataDeleteBtn.IsEnabled = isConnected;
        DataGetAllBtn.IsEnabled = isConnected;

        Log($"Connection state: {state}");
    }

    private async void ConnectBtn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ConnectBtn.IsEnabled = false;

            var serverUrl = ServerUrlBox.Text.Trim();
            if (string.IsNullOrEmpty(serverUrl))
            {
                MessageBox.Show("Please enter a server URL.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                ConnectBtn.IsEnabled = true;
                return;
            }

            _deviceInfoProvider = new TestDeviceInfoProvider
            {
                DeviceId = DeviceIdBox.Text.Trim(),
                DeviceName = DeviceNameBox.Text.Trim()
            };

            _statusProvider = new TestStatusProvider();
            _commandHandler = new TestCommandHandler();

            _commandHandler.CommandReceived += (_, args) =>
            {
                Dispatcher.BeginInvoke(() =>
                {
                    CommandsListView.Items.Add(new
                    {
                        Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        Command = args.Command,
                        Payload = args.Payload ?? "(none)"
                    });
                    Log($"Command received: {args.Command} - {args.Payload}");
                });
            };

            _loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Debug);
                builder.AddProvider(new WpfLoggerProvider(Log));
            });

            var options = new DeviceManagerClientOptions
            {
                ServerUrl = serverUrl
            };

            _client = new DeviceManagerClient(
                options,
                _deviceInfoProvider,
                _loggerFactory,
                statusProvider: _statusProvider,
                commandHandler: _commandHandler);

            _client.ConnectionStateChanged += (_, state) => UpdateConnectionState(state);

            _client.Messages.MessageReceived += (_, msg) =>
            {
                Dispatcher.BeginInvoke(() =>
                {
                    MessagesListView.Items.Add(new
                    {
                        Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        Type = msg.Type,
                        Content = msg.Content
                    });
                    Log($"Message received: [{msg.Type}] {msg.Content}");
                });
            };

            _client.Config.ConfigChanged += (_, entry) =>
            {
                Dispatcher.BeginInvoke(() =>
                {
                    Log($"Config changed: {entry.Key} = {entry.Value}");
                });
            };

            Log($"Connecting to {serverUrl} as {_deviceInfoProvider.DeviceId}...");
            await _client.ConnectAsync();
        }
        catch (Exception ex)
        {
            Log($"Connection failed: {ex.Message}");
            ConnectBtn.IsEnabled = true;
            MessageBox.Show($"Failed to connect: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void DisconnectBtn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            DisconnectBtn.IsEnabled = false;
            Log("Disconnecting...");

            if (_client is not null)
            {
                await _client.DisconnectAsync();
                await _client.DisposeAsync();
                _client = null;
            }

            _loggerFactory?.Dispose();
            _loggerFactory = null;

            UpdateConnectionState(ConnectionState.Disconnected);
            StopAutoReportBtn.IsEnabled = false;
            StartAutoReportBtn.IsEnabled = false;
        }
        catch (Exception ex)
        {
            Log($"Disconnect error: {ex.Message}");
        }
    }

    private async void SendStatusBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_client is null || _statusProvider is null) return;

        try
        {
            if (int.TryParse(StatusLevelBox.Text, out var level))
                _statusProvider.Level = level;
            if (double.TryParse(StatusProgressBox.Text, out var progress))
                _statusProvider.Progress = progress;
            if (int.TryParse(StatusBatteryBox.Text, out var battery))
                _statusProvider.Battery = battery;
            else if (string.IsNullOrWhiteSpace(StatusBatteryBox.Text))
                _statusProvider.Battery = null;

            // Start reporting once (will send immediately on next tick)
            // For a one-off, use Messages to send a status-type message
            var status = await _statusProvider.GetCurrentStatusAsync();
            await _client.Messages.SendAsync("status.report",
                $"Level={status.Level}, Progress={status.Progress}, Battery={status.Battery}");
            Log($"Status sent: Level={status.Level}, Progress={status.Progress}, Battery={status.Battery}");
        }
        catch (Exception ex)
        {
            Log($"Send status failed: {ex.Message}");
        }
    }

    private void StartAutoReportBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_client is null || _statusProvider is null) return;

        try
        {
            if (int.TryParse(StatusLevelBox.Text, out var level))
                _statusProvider.Level = level;
            if (double.TryParse(StatusProgressBox.Text, out var progress))
                _statusProvider.Progress = progress;
            if (int.TryParse(StatusBatteryBox.Text, out var battery))
                _statusProvider.Battery = battery;

            _client.StartStatusReporting(TimeSpan.FromSeconds(30));
            StartAutoReportBtn.IsEnabled = false;
            StopAutoReportBtn.IsEnabled = true;
            Log("Auto status reporting started (every 30s)");
        }
        catch (Exception ex)
        {
            Log($"Start auto-report failed: {ex.Message}");
        }
    }

    private async void StopAutoReportBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_client is null) return;

        try
        {
            await _client.StopStatusReportingAsync();
            StopAutoReportBtn.IsEnabled = false;
            StartAutoReportBtn.IsEnabled = _client.State == ConnectionState.Connected;
            Log("Auto status reporting stopped");
        }
        catch (Exception ex)
        {
            Log($"Stop auto-report failed: {ex.Message}");
        }
    }

    private async void SendMsgBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_client is null) return;

        try
        {
            var msgType = MsgTypeBox.Text.Trim();
            var content = MsgContentBox.Text.Trim();

            if (string.IsNullOrEmpty(msgType) || string.IsNullOrEmpty(content))
            {
                MessageBox.Show("Please enter message type and content.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await _client.Messages.SendAsync(msgType, content);
            Log($"Message sent: [{msgType}] {content}");
        }
        catch (Exception ex)
        {
            Log($"Send message failed: {ex.Message}");
        }
    }

    private async void RefreshConfigBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_client is null) return;

        try
        {
            ConfigStatusLabel.Content = "Loading...";
            ConfigListView.Items.Clear();

            await _client.Config.RefreshAsync();
            var configs = await _client.Config.GetAllAsync();

            foreach (var entry in configs)
            {
                ConfigListView.Items.Add(new
                {
                    entry.Key,
                    entry.Value,
                    entry.ValueType,
                    Description = entry.Description ?? ""
                });
            }

            ConfigStatusLabel.Content = $"{configs.Count} entries loaded";
            Log($"Config refreshed: {configs.Count} entries");
        }
        catch (Exception ex)
        {
            ConfigStatusLabel.Content = "Error";
            Log($"Refresh config failed: {ex.Message}");
        }
    }

    private async void DataGetBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_client is null) return;

        try
        {
            var key = DataKeyBox.Text.Trim();
            if (string.IsNullOrEmpty(key))
            {
                MessageBox.Show("Please enter a key.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var entry = await _client.DataStore.GetAsync(key);
            if (entry is not null)
            {
                DataResultText.Text = $"Key: {entry.Key}\nValue: {entry.Value}\nUpdated: {entry.UpdatedAt}";
                DataValueBox.Text = entry.Value;
                Log($"DataStore get: {entry.Key} = {entry.Value}");
            }
            else
            {
                DataResultText.Text = $"Key '{key}' not found.";
                Log($"DataStore get: '{key}' not found");
            }
        }
        catch (Exception ex)
        {
            DataResultText.Text = $"Error: {ex.Message}";
            Log($"DataStore get failed: {ex.Message}");
        }
    }

    private async void DataSetBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_client is null) return;

        try
        {
            var key = DataKeyBox.Text.Trim();
            var value = DataValueBox.Text.Trim();

            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
            {
                MessageBox.Show("Please enter both key and value.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await _client.DataStore.SetAsync(key, value);
            DataResultText.Text = $"Set '{key}' = '{value}' successfully.";
            Log($"DataStore set: {key} = {value}");
        }
        catch (Exception ex)
        {
            DataResultText.Text = $"Error: {ex.Message}";
            Log($"DataStore set failed: {ex.Message}");
        }
    }

    private async void DataDeleteBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_client is null) return;

        try
        {
            var key = DataKeyBox.Text.Trim();
            if (string.IsNullOrEmpty(key))
            {
                MessageBox.Show("Please enter a key.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await _client.DataStore.DeleteAsync(key);
            DataResultText.Text = $"Deleted key '{key}'.";
            Log($"DataStore deleted: {key}");
        }
        catch (Exception ex)
        {
            DataResultText.Text = $"Error: {ex.Message}";
            Log($"DataStore delete failed: {ex.Message}");
        }
    }

    private async void DataGetAllBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_client is null) return;

        try
        {
            DataStoreListView.Items.Clear();
            var entries = await _client.DataStore.GetAllAsync();

            foreach (var entry in entries)
            {
                DataStoreListView.Items.Add(new
                {
                    entry.Key,
                    entry.Value,
                    UpdatedAt = entry.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss")
                });
            }

            DataResultText.Text = $"{entries.Count} entries loaded.";
            Log($"DataStore get all: {entries.Count} entries");
        }
        catch (Exception ex)
        {
            DataResultText.Text = $"Error: {ex.Message}";
            Log($"DataStore get all failed: {ex.Message}");
        }
    }

    private void ClearLogBtn_Click(object sender, RoutedEventArgs e)
    {
        LogBox.Clear();
    }

    private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        try
        {
            if (_client is not null)
            {
                await _client.DisconnectAsync();
                await _client.DisposeAsync();
            }
            _loggerFactory?.Dispose();
        }
        catch
        {
            // Swallow during shutdown
        }
    }
}

/// <summary>
/// Logger provider that forwards log messages to a WPF action (for display in the log TextBox).
/// </summary>
internal sealed class WpfLoggerProvider : ILoggerProvider
{
    private readonly Action<string> _logAction;

    public WpfLoggerProvider(Action<string> logAction)
    {
        _logAction = logAction;
    }

    public ILogger CreateLogger(string categoryName) => new WpfLogger(categoryName, _logAction);

    public void Dispose() { }

    private sealed class WpfLogger : ILogger
    {
        private readonly string _category;
        private readonly Action<string> _logAction;

        public WpfLogger(string category, Action<string> logAction)
        {
            _category = category;
            _logAction = logAction;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            var shortCategory = _category.Contains('.') ? _category[((_category.LastIndexOf('.') + 1))..] : _category;
            _logAction($"[{logLevel}] {shortCategory}: {formatter(state, exception)}");
        }
    }
}
