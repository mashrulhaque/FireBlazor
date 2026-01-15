namespace FireBlazor;

/// <summary>
/// Base exception for all Firebase errors.
/// </summary>
public class FirebaseException : Exception
{
    public string Code { get; }

    public FirebaseException(string code, string message) : base(message)
    {
        Code = code;
    }

    public FirebaseException(string code, string message, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
    }
}
