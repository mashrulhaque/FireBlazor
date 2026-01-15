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
            var args = new object?[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                if (call.Arguments.TryGetProperty(param.Name!, out var value))
                {
                    args[i] = value.Deserialize(param.ParameterType);
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
        catch (Exception ex)
        {
            return FunctionResponse.FromError(call.Name, ex.Message);
        }
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
