using System.Text.Json;

namespace FireBlazor.Tests.Firestore;

public class FieldValueSerializationTests
{
    private readonly JsonSerializerOptions _options;

    public FieldValueSerializationTests()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new FieldValueConverter() }
        };
    }

    [Fact]
    public void ServerTimestamp_SerializesToSentinelObject()
    {
        var data = new { timestamp = FieldValue.ServerTimestamp() };
        var json = JsonSerializer.Serialize(data, _options);
        Assert.Contains("\"__fieldValue__\":\"serverTimestamp\"", json);
    }

    [Fact]
    public void Increment_SerializesToSentinelObject()
    {
        var data = new { count = FieldValue.Increment(5) };
        var json = JsonSerializer.Serialize(data, _options);
        Assert.Contains("\"__fieldValue__\":\"increment\"", json);
        Assert.Contains("\"value\":5", json);
    }

    [Fact]
    public void IncrementDouble_SerializesToSentinelObject()
    {
        var data = new { rating = FieldValue.Increment(0.5) };
        var json = JsonSerializer.Serialize(data, _options);
        Assert.Contains("\"__fieldValue__\":\"increment\"", json);
        Assert.Contains("\"value\":0.5", json);
    }

    [Fact]
    public void ArrayUnion_SerializesToSentinelObject()
    {
        var data = new { tags = FieldValue.ArrayUnion("a", "b") };
        var json = JsonSerializer.Serialize(data, _options);
        Assert.Contains("\"__fieldValue__\":\"arrayUnion\"", json);
        Assert.Contains("\"elements\":[\"a\",\"b\"]", json);
    }

    [Fact]
    public void ArrayRemove_SerializesToSentinelObject()
    {
        var data = new { tags = FieldValue.ArrayRemove("x") };
        var json = JsonSerializer.Serialize(data, _options);
        Assert.Contains("\"__fieldValue__\":\"arrayRemove\"", json);
        Assert.Contains("\"elements\":[\"x\"]", json);
    }

    [Fact]
    public void Delete_SerializesToSentinelObject()
    {
        var data = new { obsoleteField = FieldValue.Delete() };
        var json = JsonSerializer.Serialize(data, _options);
        Assert.Contains("\"__fieldValue__\":\"delete\"", json);
    }
}
