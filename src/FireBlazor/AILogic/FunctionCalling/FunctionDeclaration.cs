namespace FireBlazor;

/// <summary>
/// Declares a function that the AI model can call.
/// </summary>
public sealed record FunctionDeclaration
{
    /// <summary>The name of the function.</summary>
    public required string Name { get; init; }

    /// <summary>Description of what the function does.</summary>
    public required string Description { get; init; }

    /// <summary>JSON Schema for the function parameters.</summary>
    public FunctionParameters? Parameters { get; init; }
}

/// <summary>
/// JSON Schema definition for function parameters.
/// </summary>
public sealed record FunctionParameters
{
    /// <summary>The JSON Schema type (usually "object").</summary>
    public string Type { get; init; } = "object";

    /// <summary>Properties of the parameters object.</summary>
    public IReadOnlyDictionary<string, ParameterSchema>? Properties { get; init; }

    /// <summary>List of required parameter names.</summary>
    public IReadOnlyList<string>? Required { get; init; }
}

/// <summary>
/// Schema for a single parameter.
/// </summary>
public sealed record ParameterSchema
{
    /// <summary>The type of the parameter (string, number, boolean, array, object).</summary>
    public required string Type { get; init; }

    /// <summary>Description of the parameter.</summary>
    public string? Description { get; init; }

    /// <summary>Allowed values for enum parameters.</summary>
    public IReadOnlyList<string>? Enum { get; init; }

    /// <summary>Schema for array items.</summary>
    public ParameterSchema? Items { get; init; }

    /// <summary>Properties for object types.</summary>
    public IReadOnlyDictionary<string, ParameterSchema>? Properties { get; init; }
}
