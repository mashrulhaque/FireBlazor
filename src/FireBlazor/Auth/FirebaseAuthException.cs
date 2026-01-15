namespace FireBlazor;

/// <summary>
/// Exception thrown by Firebase Authentication operations.
/// </summary>
public sealed class FirebaseAuthException : FirebaseException
{
    public AuthErrorCode AuthCode { get; }

    public FirebaseAuthException(AuthErrorCode code, string message)
        : base(code.ToFirebaseCode(), message)
    {
        AuthCode = code;
    }

    public FirebaseAuthException(AuthErrorCode code, string message, Exception innerException)
        : base(code.ToFirebaseCode(), message, innerException)
    {
        AuthCode = code;
    }
}
