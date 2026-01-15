namespace FireBlazor;

/// <summary>
/// A tool that the model can use (e.g., function calling, code execution).
/// </summary>
public sealed record Tool
{
    /// <summary>
    /// Function declarations for this tool.
    /// </summary>
    public IReadOnlyList<FunctionDeclaration>? FunctionDeclarations { get; init; }

    /// <summary>
    /// Creates a tool with function declarations.
    /// </summary>
    public static Tool WithFunctions(params FunctionDeclaration[] functions) =>
        new() { FunctionDeclarations = functions };
}

/// <summary>
/// Configuration for how the model should use tools.
/// </summary>
public sealed record ToolConfig
{
    /// <summary>
    /// Mode for function calling.
    /// </summary>
    public FunctionCallingMode Mode { get; init; } = FunctionCallingMode.Auto;

    /// <summary>
    /// When Mode is Any, specifies which functions are allowed.
    /// </summary>
    public IReadOnlyList<string>? AllowedFunctionNames { get; init; }
}

/// <summary>
/// Mode for how the model should use function calling.
/// </summary>
public enum FunctionCallingMode
{
    /// <summary>Model decides when to call functions.</summary>
    Auto = 0,

    /// <summary>Model must call a function from the allowed list.</summary>
    Any = 1,

    /// <summary>Model will not call any functions.</summary>
    None = 2
}
