using System.Text.Json;

namespace FireBlazor;

/// <summary>
/// Represents a function call requested by the AI model.
/// </summary>
public sealed record FunctionCall
{
    /// <summary>The name of the function to call.</summary>
    public required string Name { get; init; }

    /// <summary>The arguments to pass to the function as JSON.</summary>
    public required JsonElement Arguments { get; init; }

    /// <summary>
    /// Gets the argument value as the specified type.
    /// </summary>
    public T? GetArgument<T>(string name)
    {
        if (Arguments.TryGetProperty(name, out var property))
        {
            return property.Deserialize<T>();
        }
        return default;
    }

    /// <summary>
    /// Tries to get the argument value as the specified type.
    /// </summary>
    public bool TryGetArgument<T>(string name, out T? value)
    {
        if (Arguments.TryGetProperty(name, out var property))
        {
            try
            {
                value = property.Deserialize<T>();
                return true;
            }
            catch
            {
                value = default;
                return false;
            }
        }
        value = default;
        return false;
    }

    /// <summary>
    /// Gets all arguments as a dictionary.
    /// </summary>
    public Dictionary<string, object?> GetAllArguments()
    {
        var result = new Dictionary<string, object?>();
        foreach (var property in Arguments.EnumerateObject())
        {
            result[property.Name] = property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString(),
                JsonValueKind.Number => property.Value.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => property.Value.GetRawText()
            };
        }
        return result;
    }
}
