using System.Text.Json;
using System.Text.Json.Serialization;

namespace FireBlazor;

/// <summary>
/// JSON converter that deserializes Firestore timestamp objects to DateTime.
/// Firestore returns timestamps as {seconds: number, nanoseconds: number} objects.
/// </summary>
public sealed class FirestoreTimestampConverter : JsonConverter<DateTime?>
{
    private static readonly DateTime UnixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        // Handle ISO date strings
        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString();
            return string.IsNullOrEmpty(str) ? null : DateTime.Parse(str);
        }

        // Handle Firestore timestamp objects: {seconds: number, nanoseconds: number}
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            long? seconds = null;
            int? nanoseconds = null;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var prop = reader.GetString();
                    reader.Read();

                    if (prop is "seconds" or "_seconds")
                        seconds = reader.GetInt64();
                    else if (prop is "nanoseconds" or "_nanoseconds")
                        nanoseconds = reader.GetInt32();
                }
            }

            if (seconds.HasValue)
            {
                var dt = UnixEpoch.AddSeconds(seconds.Value);
                if (nanoseconds.HasValue)
                    dt = dt.AddTicks(nanoseconds.Value / 100); // 1 tick = 100 nanoseconds
                return dt;
            }

            return null;
        }

        throw new JsonException($"Cannot convert token type {reader.TokenType} to DateTime");
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteStringValue(value.Value.ToString("O")); // ISO 8601 format
        else
            writer.WriteNullValue();
    }
}

/// <summary>
/// JSON converter that serializes FieldValue sentinels to a format
/// recognized by the JavaScript layer for Firestore operations.
/// </summary>
public sealed class FieldValueConverter : JsonConverter<FieldValue>
{
    public override FieldValue? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException("FieldValue deserialization is not supported");
    }

    public override void Write(Utf8JsonWriter writer, FieldValue value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        switch (value)
        {
            case ServerTimestampValue:
                writer.WriteString("__fieldValue__", "serverTimestamp");
                break;

            case IncrementValue inc:
                writer.WriteString("__fieldValue__", "increment");
                writer.WriteNumber("value", inc.Amount);
                break;

            case IncrementDoubleValue incDouble:
                writer.WriteString("__fieldValue__", "increment");
                writer.WriteNumber("value", incDouble.Amount);
                break;

            case ArrayUnionValue union:
                writer.WriteString("__fieldValue__", "arrayUnion");
                writer.WritePropertyName("elements");
                JsonSerializer.Serialize(writer, union.Elements, options);
                break;

            case ArrayRemoveValue remove:
                writer.WriteString("__fieldValue__", "arrayRemove");
                writer.WritePropertyName("elements");
                JsonSerializer.Serialize(writer, remove.Elements, options);
                break;

            case DeleteFieldValue:
                writer.WriteString("__fieldValue__", "delete");
                break;

            default:
                throw new JsonException($"Unknown FieldValue type: {value.GetType().Name}");
        }

        writer.WriteEndObject();
    }
}
