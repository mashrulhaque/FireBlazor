using FireBlazor;

namespace FireBlazor.Tests.RealtimeDb;

public class TransactionTests
{
    [Fact]
    public void TransactionResult_Committed_HasValue()
    {
        var result = new TransactionResult<int>
        {
            Committed = true,
            Value = 42
        };

        Assert.True(result.Committed);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void TransactionResult_Aborted_CommittedIsFalse()
    {
        var result = new TransactionResult<int>
        {
            Committed = false,
            Value = default
        };

        Assert.False(result.Committed);
    }

    [Fact]
    public void TransactionResult_WithComplexType_PreservesValue()
    {
        var data = new TestData { Name = "Test", Count = 5 };
        var result = new TransactionResult<TestData>
        {
            Committed = true,
            Value = data
        };

        Assert.True(result.Committed);
        Assert.Equal("Test", result.Value?.Name);
        Assert.Equal(5, result.Value?.Count);
    }

    private sealed class TestData
    {
        public string Name { get; set; } = "";
        public int Count { get; set; }
    }
}
