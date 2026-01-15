namespace FireBlazor;

/// <summary>
/// Firebase Cloud Storage error codes.
/// </summary>
public enum StorageErrorCode
{
    Unknown,
    ObjectNotFound,
    BucketNotFound,
    ProjectNotFound,
    QuotaExceeded,
    Unauthenticated,
    Unauthorized,
    RetryLimitExceeded,
    InvalidChecksum,
    Canceled,
    InvalidUrl,
    InvalidArgument,
    NoDefaultBucket,
    CannotSliceBlob,
    ServerFileWrongSize
}

public static class StorageErrorCodeExtensions
{
    public static StorageErrorCode FromFirebaseCode(string code) => code switch
    {
        "storage/object-not-found" => StorageErrorCode.ObjectNotFound,
        "storage/bucket-not-found" => StorageErrorCode.BucketNotFound,
        "storage/project-not-found" => StorageErrorCode.ProjectNotFound,
        "storage/quota-exceeded" => StorageErrorCode.QuotaExceeded,
        "storage/unauthenticated" => StorageErrorCode.Unauthenticated,
        "storage/unauthorized" => StorageErrorCode.Unauthorized,
        "storage/retry-limit-exceeded" => StorageErrorCode.RetryLimitExceeded,
        "storage/invalid-checksum" => StorageErrorCode.InvalidChecksum,
        "storage/canceled" => StorageErrorCode.Canceled,
        "storage/invalid-url" => StorageErrorCode.InvalidUrl,
        "storage/invalid-argument" => StorageErrorCode.InvalidArgument,
        "storage/no-default-bucket" => StorageErrorCode.NoDefaultBucket,
        "storage/cannot-slice-blob" => StorageErrorCode.CannotSliceBlob,
        "storage/server-file-wrong-size" => StorageErrorCode.ServerFileWrongSize,
        _ => StorageErrorCode.Unknown
    };

    public static string ToFirebaseCode(this StorageErrorCode code) => code switch
    {
        StorageErrorCode.ObjectNotFound => "storage/object-not-found",
        StorageErrorCode.BucketNotFound => "storage/bucket-not-found",
        StorageErrorCode.ProjectNotFound => "storage/project-not-found",
        StorageErrorCode.QuotaExceeded => "storage/quota-exceeded",
        StorageErrorCode.Unauthenticated => "storage/unauthenticated",
        StorageErrorCode.Unauthorized => "storage/unauthorized",
        StorageErrorCode.RetryLimitExceeded => "storage/retry-limit-exceeded",
        StorageErrorCode.InvalidChecksum => "storage/invalid-checksum",
        StorageErrorCode.Canceled => "storage/canceled",
        StorageErrorCode.InvalidUrl => "storage/invalid-url",
        StorageErrorCode.InvalidArgument => "storage/invalid-argument",
        StorageErrorCode.NoDefaultBucket => "storage/no-default-bucket",
        StorageErrorCode.CannotSliceBlob => "storage/cannot-slice-blob",
        StorageErrorCode.ServerFileWrongSize => "storage/server-file-wrong-size",
        _ => "storage/unknown"
    };
}
