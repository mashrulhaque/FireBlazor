namespace FireBlazor;

/// <summary>
/// Exception thrown by Firebase App Check operations.
/// </summary>
public sealed class AppCheckException : FirebaseException
{
    public AppCheckErrorCode AppCheckCode { get; }

    public AppCheckException(AppCheckErrorCode code, string message)
        : base(code.ToFirebaseCode(), message)
    {
        AppCheckCode = code;
    }

    public AppCheckException(AppCheckErrorCode code, string message, Exception innerException)
        : base(code.ToFirebaseCode(), message, innerException)
    {
        AppCheckCode = code;
    }
}
