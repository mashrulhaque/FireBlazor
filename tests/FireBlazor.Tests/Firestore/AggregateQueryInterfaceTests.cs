// tests/FireBlazor.Tests/Firestore/AggregateQueryInterfaceTests.cs
namespace FireBlazor.Tests.Firestore;

public class AggregateQueryInterfaceTests
{
    [Fact]
    public void IAggregateQuery_HasCountAsyncMethod()
    {
        var method = typeof(IAggregateQuery).GetMethod("CountAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<Result<long>>), method!.ReturnType);
    }

    [Fact]
    public void IAggregateQuery_HasSumAsyncMethod()
    {
        var method = typeof(IAggregateQuery).GetMethod("SumAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void IAggregateQuery_HasAverageAsyncMethod()
    {
        var method = typeof(IAggregateQuery).GetMethod("AverageAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void ICollectionReference_HasAggregateMethod()
    {
        var method = typeof(ICollectionReference<object>).GetMethod("Aggregate");
        Assert.NotNull(method);
        Assert.Equal(typeof(IAggregateQuery), method!.ReturnType);
    }
}
