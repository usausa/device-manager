namespace DeviceManager.Server.Infrastructure.Authentication;

using System.Security.Cryptography;
using System.Text.Encodings.Web;

public static class ApiKeyDefaults
{
    public const string AuthenticationScheme = "ApiKey";
}

// X-Api-Key ヘッダによる端末向けの共有キー認証
public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly DeviceSetting setting;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        DeviceSetting setting)
        : base(options, logger, encoder)
    {
        this.setting = setting;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiConstants.ApiKeyHeader, out var values))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        // タイミング攻撃対策として固定時間比較を使用する
        var keyBytes = Encoding.UTF8.GetBytes(values.ToString());
        var expectedBytes = Encoding.UTF8.GetBytes(setting.ApiKey);
        if ((keyBytes.Length != expectedBytes.Length) || !CryptographicOperations.FixedTimeEquals(keyBytes, expectedBytes))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid api key."));
        }

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "device")], Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
