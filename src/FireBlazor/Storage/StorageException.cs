namespace FireBlazor;

/// <summary>
/// Exception thrown by Firebase Storage operations.
/// </summary>
public sealed class StorageException : FirebaseException
{
    public StorageErrorCode StorageCode { get; }

    public StorageException(StorageErrorCode code, string message)
        : base(code.ToFirebaseCode(), message)
    {
        StorageCode = code;
    }

    public StorageException(StorageErrorCode code, string message, Exception innerException)
        : base(code.ToFirebaseCode(), message, innerException)
    {
        StorageCode = code;
    }
}
