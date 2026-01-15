namespace FireBlazor;

/// <summary>
/// Firebase AI Logic error codes.
/// </summary>
public enum AILogicErrorCode
{
    Unknown,
    InvalidApiKey,
    QuotaExceeded,
    ModelNotFound,
    InvalidRequest,
    ContentBlocked,
    NetworkError,
    Timeout,
    ServiceUnavailable
}

public static class AILogicErrorCodeExtensions
{
    public static AILogicErrorCode FromFirebaseCode(string code) => code switch
    {
        "ai/invalid-api-key" => AILogicErrorCode.InvalidApiKey,
        "ai/quota-exceeded" => AILogicErrorCode.QuotaExceeded,
        "ai/model-not-found" => AILogicErrorCode.ModelNotFound,
        "ai/invalid-request" => AILogicErrorCode.InvalidRequest,
        "ai/content-blocked" => AILogicErrorCode.ContentBlocked,
        "ai/network-error" => AILogicErrorCode.NetworkError,
        "ai/timeout" => AILogicErrorCode.Timeout,
        "ai/service-unavailable" => AILogicErrorCode.ServiceUnavailable,
        _ => AILogicErrorCode.Unknown
    };

    public static string ToFirebaseCode(this AILogicErrorCode code) => code switch
    {
        AILogicErrorCode.InvalidApiKey => "ai/invalid-api-key",
        AILogicErrorCode.QuotaExceeded => "ai/quota-exceeded",
        AILogicErrorCode.ModelNotFound => "ai/model-not-found",
        AILogicErrorCode.InvalidRequest => "ai/invalid-request",
        AILogicErrorCode.ContentBlocked => "ai/content-blocked",
        AILogicErrorCode.NetworkError => "ai/network-error",
        AILogicErrorCode.Timeout => "ai/timeout",
        AILogicErrorCode.ServiceUnavailable => "ai/service-unavailable",
        _ => "ai/unknown"
    };
}
