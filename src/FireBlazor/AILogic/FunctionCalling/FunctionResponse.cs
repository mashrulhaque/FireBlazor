namespace FireBlazor;

/// <summary>
/// Response to provide back to the model after executing a function.
/// </summary>
public sealed record FunctionResponse
{
    /// <summary>The name of the function that was called.</summary>
    public required string Name { get; init; }

    /// <summary>The response data from the function execution.</summary>
    public required object Response { get; init; }

    /// <summary>
    /// Creates a function response with a string result.
    /// </summary>
    public static FunctionResponse FromString(string name, string result) =>
        new() { Name = name, Response = result };

    /// <summary>
    /// Creates a function response with a structured object result.
    /// </summary>
    public static FunctionResponse FromObject(string name, object result) =>
        new() { Name = name, Response = result };

    /// <summary>
    /// Creates a function response indicating an error.
    /// </summary>
    public static FunctionResponse FromError(string name, string errorMessage) =>
        new() { Name = name, Response = new { error = errorMessage } };
}
