using System.Text.Json;
using FireBlazor;

namespace FireBlazor.Tests.RealtimeDb;

public class ServerValueTests
{
    [Fact]
    public void ServerTimestamp_SerializesToSentinel()
    {
        var data = new { timestamp = ServerValue.Timestamp };
        var json = JsonSerializer.Serialize(data, DatabaseJsonOptions.Default);

        Assert.Contains("\"__serverValue__\":\"timestamp\"", json);
    }

    [Fact]
    public void ServerIncrement_SerializesToSentinelWithDelta()
    {
        var data = new { count = ServerValue.Increment(5) };
        var json = JsonSerializer.Serialize(data, DatabaseJsonOptions.Default);

        Assert.Contains("\"__serverValue__\":\"increment\"", json);
        Assert.Contains("\"delta\":5", json);
    }

    [Fact]
    public void ServerIncrement_NegativeDelta_SerializesCorrectly()
    {
        var data = new { count = ServerValue.Increment(-3) };
        var json = JsonSerializer.Serialize(data, DatabaseJsonOptions.Default);

        Assert.Contains("\"delta\":-3", json);
    }
}
