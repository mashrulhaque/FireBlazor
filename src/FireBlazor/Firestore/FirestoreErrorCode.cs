namespace FireBlazor;

/// <summary>
/// Firestore error codes.
/// </summary>
public enum FirestoreErrorCode
{
    Unknown,
    Cancelled,
    InvalidArgument,
    NotFound,
    AlreadyExists,
    PermissionDenied,
    ResourceExhausted,
    FailedPrecondition,
    Aborted,
    OutOfRange,
    Unimplemented,
    Internal,
    Unavailable,
    DataLoss,
    Unauthenticated
}

public static class FirestoreErrorCodeExtensions
{
    public static FirestoreErrorCode FromFirebaseCode(string code) => code switch
    {
        "cancelled" => FirestoreErrorCode.Cancelled,
        "invalid-argument" => FirestoreErrorCode.InvalidArgument,
        "not-found" => FirestoreErrorCode.NotFound,
        "already-exists" => FirestoreErrorCode.AlreadyExists,
        "permission-denied" => FirestoreErrorCode.PermissionDenied,
        "resource-exhausted" => FirestoreErrorCode.ResourceExhausted,
        "failed-precondition" => FirestoreErrorCode.FailedPrecondition,
        "aborted" => FirestoreErrorCode.Aborted,
        "out-of-range" => FirestoreErrorCode.OutOfRange,
        "unimplemented" => FirestoreErrorCode.Unimplemented,
        "internal" => FirestoreErrorCode.Internal,
        "unavailable" => FirestoreErrorCode.Unavailable,
        "data-loss" => FirestoreErrorCode.DataLoss,
        "unauthenticated" => FirestoreErrorCode.Unauthenticated,
        _ => FirestoreErrorCode.Unknown
    };

    public static string ToFirebaseCode(this FirestoreErrorCode code) => code switch
    {
        FirestoreErrorCode.Cancelled => "cancelled",
        FirestoreErrorCode.InvalidArgument => "invalid-argument",
        FirestoreErrorCode.NotFound => "not-found",
        FirestoreErrorCode.AlreadyExists => "already-exists",
        FirestoreErrorCode.PermissionDenied => "permission-denied",
        FirestoreErrorCode.ResourceExhausted => "resource-exhausted",
        FirestoreErrorCode.FailedPrecondition => "failed-precondition",
        FirestoreErrorCode.Aborted => "aborted",
        FirestoreErrorCode.OutOfRange => "out-of-range",
        FirestoreErrorCode.Unimplemented => "unimplemented",
        FirestoreErrorCode.Internal => "internal",
        FirestoreErrorCode.Unavailable => "unavailable",
        FirestoreErrorCode.DataLoss => "data-loss",
        FirestoreErrorCode.Unauthenticated => "unauthenticated",
        _ => "unknown"
    };
}
