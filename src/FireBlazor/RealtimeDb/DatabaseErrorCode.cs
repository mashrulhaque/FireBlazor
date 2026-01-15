namespace FireBlazor;

/// <summary>
/// Firebase Realtime Database error codes.
/// </summary>
public enum DatabaseErrorCode
{
    Unknown,
    PermissionDenied,
    Disconnected,
    ExpiredToken,
    InvalidToken,
    MaxRetries,
    NetworkError,
    OperationFailed,
    OverriddenBySet,
    Unavailable,
    UserCodeException,
    WriteCanceled
}

public static class DatabaseErrorCodeExtensions
{
    public static DatabaseErrorCode FromFirebaseCode(string code) => code switch
    {
        "PERMISSION_DENIED" or "database/permission-denied" => DatabaseErrorCode.PermissionDenied,
        "DISCONNECTED" or "database/disconnected" => DatabaseErrorCode.Disconnected,
        "EXPIRED_TOKEN" or "database/expired-token" => DatabaseErrorCode.ExpiredToken,
        "INVALID_TOKEN" or "database/invalid-token" => DatabaseErrorCode.InvalidToken,
        "MAX_RETRIES" or "database/max-retries" => DatabaseErrorCode.MaxRetries,
        "NETWORK_ERROR" or "database/network-error" => DatabaseErrorCode.NetworkError,
        "OPERATION_FAILED" or "database/operation-failed" => DatabaseErrorCode.OperationFailed,
        "OVERRIDDEN_BY_SET" or "database/overridden-by-set" => DatabaseErrorCode.OverriddenBySet,
        "UNAVAILABLE" or "database/unavailable" => DatabaseErrorCode.Unavailable,
        "USER_CODE_EXCEPTION" or "database/user-code-exception" => DatabaseErrorCode.UserCodeException,
        "WRITE_CANCELED" or "database/write-canceled" => DatabaseErrorCode.WriteCanceled,
        _ => DatabaseErrorCode.Unknown
    };

    public static string ToFirebaseCode(this DatabaseErrorCode code) => code switch
    {
        DatabaseErrorCode.PermissionDenied => "database/permission-denied",
        DatabaseErrorCode.Disconnected => "database/disconnected",
        DatabaseErrorCode.ExpiredToken => "database/expired-token",
        DatabaseErrorCode.InvalidToken => "database/invalid-token",
        DatabaseErrorCode.MaxRetries => "database/max-retries",
        DatabaseErrorCode.NetworkError => "database/network-error",
        DatabaseErrorCode.OperationFailed => "database/operation-failed",
        DatabaseErrorCode.OverriddenBySet => "database/overridden-by-set",
        DatabaseErrorCode.Unavailable => "database/unavailable",
        DatabaseErrorCode.UserCodeException => "database/user-code-exception",
        DatabaseErrorCode.WriteCanceled => "database/write-canceled",
        _ => "database/unknown"
    };
}
