namespace DeviceManager.Client.Telemetry;

// テレメトリの送信トランスポート(失敗は false。例外は投げない)
internal interface ITelemetryTransport : IDisposable
{
    ValueTask<bool> SendAsync(IEnumerable<TelemetryEnvelope> envelopes, CancellationToken cancellationToken = default);
}
