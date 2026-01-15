namespace FireBlazor;

/// <summary>
/// Firebase App Check error codes.
/// </summary>
public enum AppCheckErrorCode
{
    Unknown,
    NotInitialized,
    FetchNetworkError,
    FetchParseError,
    FetchStatusError,
    InvalidConfiguration,
    StorageOpen,
    StorageGet,
    StorageWrite,
    RecaptchaError,
    Throttled,
    TokenExpired,
    AttestationFailed,
    TooManyRequests
}

public static class AppCheckErrorCodeExtensions
{
    public static AppCheckErrorCode FromFirebaseCode(string code) => code switch
    {
        "appCheck/not-initialized" => AppCheckErrorCode.NotInitialized,
        "appCheck/fetch-network-error" => AppCheckErrorCode.FetchNetworkError,
        "appCheck/fetch-parse-error" => AppCheckErrorCode.FetchParseError,
        "appCheck/fetch-status-error" => AppCheckErrorCode.FetchStatusError,
        "appCheck/invalid-configuration" => AppCheckErrorCode.InvalidConfiguration,
        "appCheck/storage-open" => AppCheckErrorCode.StorageOpen,
        "appCheck/storage-get" => AppCheckErrorCode.StorageGet,
        "appCheck/storage-write" => AppCheckErrorCode.StorageWrite,
        "appCheck/recaptcha-error" => AppCheckErrorCode.RecaptchaError,
        "appCheck/throttled" => AppCheckErrorCode.Throttled,
        "appCheck/token-expired" => AppCheckErrorCode.TokenExpired,
        "appCheck/attestation-failed" => AppCheckErrorCode.AttestationFailed,
        "appCheck/too-many-requests" => AppCheckErrorCode.TooManyRequests,
        _ => AppCheckErrorCode.Unknown
    };

    public static string ToFirebaseCode(this AppCheckErrorCode code) => code switch
    {
        AppCheckErrorCode.NotInitialized => "appCheck/not-initialized",
        AppCheckErrorCode.FetchNetworkError => "appCheck/fetch-network-error",
        AppCheckErrorCode.FetchParseError => "appCheck/fetch-parse-error",
        AppCheckErrorCode.FetchStatusError => "appCheck/fetch-status-error",
        AppCheckErrorCode.InvalidConfiguration => "appCheck/invalid-configuration",
        AppCheckErrorCode.StorageOpen => "appCheck/storage-open",
        AppCheckErrorCode.StorageGet => "appCheck/storage-get",
        AppCheckErrorCode.StorageWrite => "appCheck/storage-write",
        AppCheckErrorCode.RecaptchaError => "appCheck/recaptcha-error",
        AppCheckErrorCode.Throttled => "appCheck/throttled",
        AppCheckErrorCode.TokenExpired => "appCheck/token-expired",
        AppCheckErrorCode.AttestationFailed => "appCheck/attestation-failed",
        AppCheckErrorCode.TooManyRequests => "appCheck/too-many-requests",
        _ => "appCheck/unknown"
    };
}
