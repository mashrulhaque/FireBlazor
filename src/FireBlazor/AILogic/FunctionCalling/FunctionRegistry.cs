using System.Reflection;
using System.Text.Json;

namespace FireBlazor;

/// <summary>
/// Registry for C# methods that can be called by the AI model.
/// Uses reflection to discover and invoke methods marked with AIFunctionAttribute.
/// </summary>
public sealed class FunctionRegistry
{
    private readonly Dictionary<string, RegisteredFunction> _functions = new();

    /// <summary>
    /// Gets all registered function declarations for use in GenerationConfig.
    /// </summary>
    public IReadOnlyList<FunctionDeclaration> GetDeclarations() =>
        _functions.Values.Select(f => f.Declaration).ToList();

    /// <summary>
    /// Registers all methods marked with AIFunctionAttribute from an object.
    /// </summary>
    /// <param name="instance">The object containing AI functions.</param>
    public void RegisterInstance(object instance)
    {
        var type = instance.GetType();
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.GetCustomAttribute<AIFunctionAttribute>() != null);

        foreach (var method in methods)
        {
            Register(method, instance);
        }
    }

    /// <summary>
    /// Registers all static methods marked with AIFunctionAttribute from a type.
    /// </summary>
    /// <typeparam name="T">The type containing static AI functions.</typeparam>
    public void RegisterStatic<T>()
    {
        RegisterStatic(typeof(T));
    }

    /// <summary>
    /// Registers all static methods marked with AIFunctionAttribute from a type.
    /// </summary>
    public void RegisterStatic(Type type)
    {
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.GetCustomAttribute<AIFunctionAttribute>() != null);

        foreach (var method in methods)
        {
            Register(method, null);
        }
    }

    private void Register(MethodInfo method, object? instance)
    {
        var attr = method.GetCustomAttribute<AIFunctionAttribute>()!;
        var name = attr.Name ?? method.Name;

        var declaration = new FunctionDeclaration
        {
            Name = name,
            Description = attr.Description,
            Parameters = BuildParameters(method)
        };

        _functions[name] = new RegisteredFunction(declaration, method, instance);
    }

    private static FunctionParameters BuildParameters(MethodInfo method)
    {
        var parameters = method.GetParameters();
        if (parameters.Length == 0)
            return new FunctionParameters { Type = "object" };

        var properties = new Dictionary<string, ParameterSchema>();
        var required = new List<string>();

        foreach (var param in parameters)
        {
            var paramAttr = param.GetCustomAttribute<AIParameterAttribute>();
            var schema = new ParameterSchema
            {
                Type = GetJsonType(param.ParameterType),
                Description = paramAttr?.Description
            };

            properties[param.Name!] = schema;

            if (paramAttr?.Required != false && !IsNullable(param))
            {
                required.Add(param.Name!);
            }
        }

        return new FunctionParameters
        {
            Type = "object",
            Properties = properties,
            Required = required.Count > 0 ? required : null
        };
    }

    private static string GetJsonType(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (underlying == typeof(string)) return "string";
        if (underlying == typeof(int) || underlying == typeof(long) ||
            underlying == typeof(float) || underlying == typeof(double) ||
            underlying == typeof(decimal)) return "number";
        if (underlying == typeof(bool)) return "boolean";
        if (underlying.IsArray || typeof(System.Collections.IEnumerable).IsAssignableFrom(underlying) && underlying != typeof(string)) return "array";
        return "object";
    }

    private static bool IsNullable(ParameterInfo param)
    {
        if (Nullable.GetUnderlyingType(param.ParameterType) != null)
            return true;

        var context = new NullabilityInfoContext();
        var info = context.Create(param);
        return info.WriteState == NullabilityState.Nullable;
    }

    /// <summary>
    /// Executes a function call and returns the response.
    /// </summary>
    public async Task<FunctionResponse> ExecuteAsync(FunctionCall call)
    {
        if (!_functions.TryGetValue(call.Name, out var registered))
        {
            return FunctionResponse.FromError(call.Name, $"Function '{call.Name}' not found");
        }

        try
        {
            var parameters = registered.Method.GetParameters();
            var requiredParams = registered.Declaration.Parameters?.Required ?? new List<string>();
            var args = new object?[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                var paramName = param.Name!;
                var paramAttr = param.GetCustomAttribute<AIParameterAttribute>();

                if (call.Arguments.TryGetProperty(paramName, out var value))
                {
                    // Validate and deserialize the parameter value
                    var validationResult = ValidateAndDeserializeArgument(
                        paramName, value, param.ParameterType, paramAttr);

                    if (!validationResult.IsValid)
                    {
                        return FunctionResponse.FromError(call.Name, validationResult.ErrorMessage!);
                    }

                    args[i] = validationResult.Value;
                }
                else if (requiredParams.Contains(paramName))
                {
                    // Required parameter is missing
                    return FunctionResponse.FromError(call.Name,
                        $"Required parameter '{paramName}' is missing");
                }
                else if (param.HasDefaultValue)
                {
                    args[i] = param.DefaultValue;
                }
            }

            var result = registered.Method.Invoke(registered.Instance, args);

            if (result is Task task)
            {
                await task;
                var resultProperty = task.GetType().GetProperty("Result");
                result = resultProperty?.GetValue(task);
            }

            return FunctionResponse.FromObject(call.Name, result ?? "Function completed successfully");
        }
        catch (TargetInvocationException)
        {
            // Do not expose internal details from invocation exceptions
            return FunctionResponse.FromError(call.Name, "Function execution failed");
        }
        catch (Exception)
        {
            // Return sanitized error message without exposing internal details
            return FunctionResponse.FromError(call.Name, "An error occurred while executing the function");
        }
    }

    /// <summary>
    /// Validates and deserializes a single argument value.
    /// </summary>
    private static ArgumentValidationResult ValidateAndDeserializeArgument(
        string paramName,
        JsonElement value,
        Type targetType,
        AIParameterAttribute? paramAttr)
    {
        object? deserializedValue;

        // Attempt deserialization with proper error handling
        try
        {
            deserializedValue = value.Deserialize(targetType);
        }
        catch (JsonException)
        {
            return ArgumentValidationResult.Invalid(
                $"Parameter '{paramName}' has an invalid format");
        }
        catch (InvalidOperationException)
        {
            return ArgumentValidationResult.Invalid(
                $"Parameter '{paramName}' could not be converted to the expected type");
        }
        catch (Exception)
        {
            return ArgumentValidationResult.Invalid(
                $"Parameter '{paramName}' is invalid");
        }

        // Validate numeric bounds if constraints are specified
        if (paramAttr != null && deserializedValue != null)
        {
            var boundsError = ValidateNumericBounds(paramName, deserializedValue, paramAttr);
            if (boundsError != null)
            {
                return ArgumentValidationResult.Invalid(boundsError);
            }
        }

        return ArgumentValidationResult.Valid(deserializedValue);
    }

    /// <summary>
    /// Validates that a numeric value is within specified bounds.
    /// </summary>
    private static string? ValidateNumericBounds(
        string paramName,
        object value,
        AIParameterAttribute paramAttr)
    {
        // Only validate if bounds are explicitly set
        if (!paramAttr.HasMinValue && !paramAttr.HasMaxValue)
        {
            return null;
        }

        double? numericValue = value switch
        {
            int i => i,
            long l => l,
            float f => f,
            double d => d,
            decimal m => (double)m,
            _ => null
        };

        if (numericValue == null)
        {
            return null; // Not a numeric type, skip bounds validation
        }

        if (paramAttr.HasMinValue && numericValue < paramAttr.MinValue)
        {
            return $"Parameter '{paramName}' value is below the minimum allowed value";
        }

        if (paramAttr.HasMaxValue && numericValue > paramAttr.MaxValue)
        {
            return $"Parameter '{paramName}' value exceeds the maximum allowed value";
        }

        return null;
    }

    /// <summary>
    /// Result of argument validation.
    /// </summary>
    private readonly struct ArgumentValidationResult
    {
        public bool IsValid { get; }
        public object? Value { get; }
        public string? ErrorMessage { get; }

        private ArgumentValidationResult(bool isValid, object? value, string? errorMessage)
        {
            IsValid = isValid;
            Value = value;
            ErrorMessage = errorMessage;
        }

        public static ArgumentValidationResult Valid(object? value) =>
            new(true, value, null);

        public static ArgumentValidationResult Invalid(string errorMessage) =>
            new(false, null, errorMessage);
    }

    /// <summary>
    /// Checks if a function is registered.
    /// </summary>
    public bool HasFunction(string name) => _functions.ContainsKey(name);

    private sealed record RegisteredFunction(
        FunctionDeclaration Declaration,
        MethodInfo Method,
        object? Instance);
}
