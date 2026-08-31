namespace DeviceManager.Client;

using Microsoft.AspNetCore.SignalR.Client;

// 指数バックオフの再接続ポリシー(上限で頭打ち)
internal sealed class BackoffRetryPolicy : IRetryPolicy
{
    private readonly TimeSpan maxInterval;

    public BackoffRetryPolicy(TimeSpan maxInterval)
    {
        this.maxInterval = maxInterval;
    }

    public TimeSpan? NextRetryDelay(RetryContext retryContext)
    {
        var seconds = Math.Min(maxInterval.TotalSeconds, Math.Pow(2, retryContext.PreviousRetryCount));
        return TimeSpan.FromSeconds(seconds);
    }
}
