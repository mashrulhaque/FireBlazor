namespace FireBlazor;

/// <summary>
/// Exception thrown by Firestore operations.
/// </summary>
public sealed class FirestoreException : FirebaseException
{
    public FirestoreErrorCode FirestoreCode { get; }

    public FirestoreException(FirestoreErrorCode code, string message)
        : base(code.ToFirebaseCode(), message)
    {
        FirestoreCode = code;
    }

    public FirestoreException(FirestoreErrorCode code, string message, Exception innerException)
        : base(code.ToFirebaseCode(), message, innerException)
    {
        FirestoreCode = code;
    }
}
