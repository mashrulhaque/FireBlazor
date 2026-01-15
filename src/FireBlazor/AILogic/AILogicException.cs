namespace FireBlazor;

/// <summary>
/// Exception thrown by Firebase AI Logic operations.
/// </summary>
public sealed class AILogicException : FirebaseException
{
    public AILogicErrorCode AICode { get; }

    public AILogicException(AILogicErrorCode code, string message)
        : base(code.ToFirebaseCode(), message)
    {
        AICode = code;
    }

    public AILogicException(AILogicErrorCode code, string message, Exception innerException)
        : base(code.ToFirebaseCode(), message, innerException)
    {
        AICode = code;
    }
}
