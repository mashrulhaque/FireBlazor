using FireBlazor.Platform.Wasm;
using Microsoft.JSInterop;
using NSubstitute;

namespace FireBlazor.Tests.Firestore;

public class WasmAggregateQueryTests
{
    public class Product
    {
        public string? Name { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }

    [Fact]
    public void Collection_Aggregate_ReturnsAggregateQuery()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var firestore = new WasmFirestore(jsInterop);

        var aggregate = firestore.Collection<Product>("products").Aggregate();

        Assert.NotNull(aggregate);
        Assert.IsAssignableFrom<IAggregateQuery>(aggregate);
    }

    [Fact]
    public void FilteredCollection_Aggregate_ReturnsAggregateQuery()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var firestore = new WasmFirestore(jsInterop);

        var aggregate = firestore.Collection<Product>("products")
            .Where(p => p.Price > 100)
            .Aggregate();

        Assert.NotNull(aggregate);
    }

    [Fact]
    public void Aggregate_WithOrderByAndLimit_PreservesQueryParams()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var firestore = new WasmFirestore(jsInterop);

        var aggregate = firestore.Collection<Product>("products")
            .Where(p => p.Quantity > 0)
            .OrderBy(p => p.Name)
            .Take(100)
            .Aggregate();

        Assert.NotNull(aggregate);
        Assert.IsAssignableFrom<IAggregateQuery>(aggregate);
    }

    [Fact]
    public void Aggregate_FromChainedQueries_ReturnsAggregateQuery()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var firestore = new WasmFirestore(jsInterop);

        var aggregate = firestore.Collection<Product>("products")
            .Where(p => p.Price >= 10)
            .Where(p => p.Price <= 100)
            .Aggregate();

        Assert.NotNull(aggregate);
    }

    [Fact]
    public async Task SumAsync_ThrowsOnNullField()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var aggregate = new WasmAggregateQuery(jsInterop, "products", null);

        await Assert.ThrowsAsync<ArgumentNullException>(() => aggregate.SumAsync(null!));
    }

    [Fact]
    public async Task SumAsync_ThrowsOnEmptyField()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var aggregate = new WasmAggregateQuery(jsInterop, "products", null);

        await Assert.ThrowsAsync<ArgumentException>(() => aggregate.SumAsync(""));
    }

    [Fact]
    public async Task AverageAsync_ThrowsOnNullField()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var aggregate = new WasmAggregateQuery(jsInterop, "products", null);

        await Assert.ThrowsAsync<ArgumentNullException>(() => aggregate.AverageAsync(null!));
    }

    [Fact]
    public async Task AverageAsync_ThrowsOnEmptyField()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var aggregate = new WasmAggregateQuery(jsInterop, "products", null);

        await Assert.ThrowsAsync<ArgumentException>(() => aggregate.AverageAsync(""));
    }

    [Fact]
    public void Constructor_ThrowsOnNullJsInterop()
    {
        Assert.Throws<ArgumentNullException>(() => new WasmAggregateQuery(null!, "products", null));
    }

    [Fact]
    public void Constructor_ThrowsOnNullPath()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);

        Assert.Throws<ArgumentNullException>(() => new WasmAggregateQuery(jsInterop, null!, null));
    }

    [Fact]
    public void Constructor_ThrowsOnEmptyPath()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);

        Assert.Throws<ArgumentException>(() => new WasmAggregateQuery(jsInterop, "", null));
    }
}
