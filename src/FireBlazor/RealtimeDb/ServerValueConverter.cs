using System.Text.Json;
using System.Text.Json.Serialization;

namespace FireBlazor;

/// <summary>
/// JSON converter for ServerValue types (Timestamp, Increment).
/// </summary>
internal sealed class ServerValueConverter : JsonConverter<object>
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert == typeof(RtdbServerTimestampValue) ||
               typeToConvert == typeof(RtdbIncrementValue);
    }

    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException("ServerValue types cannot be deserialized");
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case RtdbServerTimestampValue:
                writer.WriteStartObject();
                writer.WriteString("__serverValue__", "timestamp");
                writer.WriteEndObject();
                break;

            case RtdbIncrementValue inc:
                writer.WriteStartObject();
                writer.WriteString("__serverValue__", "increment");
                writer.WriteNumber("delta", inc.Delta);
                writer.WriteEndObject();
                break;

            default:
                throw new JsonException($"Unexpected ServerValue type: {value.GetType()}");
        }
    }
}

/// <summary>
/// JSON options configured for Realtime Database operations with ServerValue support.
/// </summary>
public static class DatabaseJsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new ServerValueConverter() }
    };
}
