using System.Text.Json;

namespace FireBlazor.Platform.Wasm;

/// <summary>
/// Shared JSON serialization options for Firestore operations.
/// </summary>
internal static class FirestoreJsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new FieldValueConverter(), new FirestoreTimestampConverter() }
    };
}

/// <summary>
/// Shared helper for parsing Firestore document snapshots from JSON.
/// </summary>
internal static class SnapshotParser
{
    public static DocumentSnapshot<T> Parse<T>(JsonElement item, string? fallbackId = null, string? fallbackPath = null) where T : class
    {
        var id = item.TryGetProperty("id", out var idElement) ? idElement.GetString() ?? fallbackId ?? "" : fallbackId ?? "";
        var path = item.TryGetProperty("path", out var pathElement) ? pathElement.GetString() ?? fallbackPath ?? "" : fallbackPath ?? "";
        var exists = item.TryGetProperty("exists", out var existsElement) && existsElement.GetBoolean();

        T? docData = default;
        SnapshotMetadata? metadata = null;

        if (exists && item.TryGetProperty("data", out var dataElement))
        {
            docData = JsonSerializer.Deserialize<T>(dataElement.GetRawText(), FirestoreJsonOptions.Default);
        }

        if (item.TryGetProperty("metadata", out var metaElement))
        {
            metadata = new SnapshotMetadata
            {
                IsFromCache = metaElement.TryGetProperty("isFromCache", out var fromCache) && fromCache.GetBoolean(),
                HasPendingWrites = metaElement.TryGetProperty("hasPendingWrites", out var pending) && pending.GetBoolean()
            };
        }

        return new DocumentSnapshot<T>
        {
            Id = id,
            Path = path,
            Exists = exists,
            Data = docData,
            Metadata = metadata
        };
    }

    public static IReadOnlyList<DocumentSnapshot<T>> ParseMany<T>(JsonElement data) where T : class
    {
        var snapshots = new List<DocumentSnapshot<T>>();

        if (data.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in data.EnumerateArray())
            {
                snapshots.Add(Parse<T>(item));
            }
        }
        else if (data.ValueKind == JsonValueKind.Object)
        {
            snapshots.Add(Parse<T>(data));
        }

        return snapshots;
    }
}

/// <summary>
/// Shared helper for converting property names to camelCase for JavaScript interop.
/// </summary>
internal static class CamelCaseHelper
{
    public static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        return char.ToLowerInvariant(name[0]) + name[1..];
    }
}
