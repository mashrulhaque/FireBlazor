namespace FireBlazor;

/// <summary>
/// Marks a method as callable by the AI model.
/// The method must be public and can be static or instance.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class AIFunctionAttribute : Attribute
{
    /// <summary>
    /// The name the AI model uses to call this function.
    /// Defaults to the method name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Description of what the function does for the AI model.
    /// </summary>
    public required string Description { get; set; }
}

/// <summary>
/// Provides description for a parameter of an AI function.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class AIParameterAttribute : Attribute
{
    /// <summary>
    /// Description of the parameter for the AI model.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Whether this parameter is required.
    /// Defaults to true for non-nullable reference types.
    /// </summary>
    public bool Required { get; set; } = true;

    /// <summary>
    /// Minimum value constraint for numeric parameters.
    /// Only applies to numeric types (int, long, float, double, decimal).
    /// </summary>
    public double MinValue { get; set; } = double.MinValue;

    /// <summary>
    /// Maximum value constraint for numeric parameters.
    /// Only applies to numeric types (int, long, float, double, decimal).
    /// </summary>
    public double MaxValue { get; set; } = double.MaxValue;

    /// <summary>
    /// Indicates whether bounds constraints have been explicitly set.
    /// </summary>
    internal bool HasMinValue => MinValue != double.MinValue;

    /// <summary>
    /// Indicates whether bounds constraints have been explicitly set.
    /// </summary>
    internal bool HasMaxValue => MaxValue != double.MaxValue;
}
