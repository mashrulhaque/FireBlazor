using System.Text.Json;
using FireBlazor.Platform.Wasm;

namespace FireBlazor.Tests.Firestore;

public class FieldValueIntegrationTests
{
    [Fact]
    public void FirestoreJsonOptions_ContainsFieldValueConverter()
    {
        var options = FirestoreJsonOptions.Default;
        var hasConverter = options.Converters.Any(c => c is FieldValueConverter);
        Assert.True(hasConverter, "FirestoreJsonOptions should include FieldValueConverter");
    }

    [Fact]
    public void SerializeWithFirestoreOptions_HandlesFieldValues()
    {
        var data = new { lastUpdated = FieldValue.ServerTimestamp() };
        var json = JsonSerializer.Serialize(data, FirestoreJsonOptions.Default);
        Assert.Contains("__fieldValue__", json);
    }
}
