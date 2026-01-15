using System.Text.Json;
using Microsoft.JSInterop;

namespace FireBlazor.Platform.Wasm;

/// <summary>
/// Handles the callback from JavaScript during a transaction.
/// </summary>
internal sealed class DatabaseTransactionHandler<T> : ITransactionCallback
{
    private readonly Func<T?, T?> _updateFunction;
    private static readonly JsonSerializerOptions JsonOptions = DatabaseJsonOptions.Default;

    public DatabaseTransactionHandler(Func<T?, T?> updateFunction)
    {
        _updateFunction = updateFunction;
    }

    [JSInvokable]
    public object? OnTransactionUpdate(JsonElement? currentData)
    {
        T? currentValue = default;

        if (currentData.HasValue && currentData.Value.ValueKind != JsonValueKind.Null)
        {
            currentValue = JsonSerializer.Deserialize<T>(currentData.Value.GetRawText(), JsonOptions);
        }

        var newValue = _updateFunction(currentValue);

        // null means abort the transaction
        if (newValue == null)
        {
            return null;
        }

        // Serialize with ServerValue support
        return JsonSerializer.SerializeToElement(newValue, JsonOptions);
    }
}
