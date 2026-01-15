namespace FireBlazor;

/// <summary>
/// Extension methods for Result&lt;T&gt; to support exception-style error handling.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Returns the value if successful, otherwise throws FirebaseException.
    /// </summary>
    public static T OrThrow<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return result.Value;

        throw new FirebaseException(result.Error!.Code, result.Error.Message);
    }

    /// <summary>
    /// Awaits the task and returns the value if successful, otherwise throws FirebaseException.
    /// </summary>
    public static async Task<T> OrThrow<T>(this Task<Result<T>> resultTask)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.OrThrow();
    }

    /// <summary>
    /// Awaits the ValueTask and returns the value if successful, otherwise throws FirebaseException.
    /// </summary>
    public static async ValueTask<T> OrThrow<T>(this ValueTask<Result<T>> resultTask)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.OrThrow();
    }
}
