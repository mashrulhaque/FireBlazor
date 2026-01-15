using FireBlazor.Platform.Wasm;
using Microsoft.JSInterop;
using NSubstitute;

namespace FireBlazor.Tests.Firestore;

public class WasmFirestoreTests
{
    [Fact]
    public void Collection_ReturnsCollectionReference()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var firestore = new WasmFirestore(jsInterop);

        var collection = firestore.Collection<TestDocument>("users");

        Assert.NotNull(collection);
    }

    [Fact]
    public void Collection_WithPath_ReturnsCorrectType()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var firestore = new WasmFirestore(jsInterop);

        var collection = firestore.Collection<TestDocument>("users");

        Assert.IsAssignableFrom<ICollectionReference<TestDocument>>(collection);
    }

    [Fact]
    public void Collection_NestedPath_ReturnsCollectionReference()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var firestore = new WasmFirestore(jsInterop);

        var collection = firestore.Collection<TestDocument>("users/abc123/posts");

        Assert.NotNull(collection);
    }

    [Fact]
    public void Collection_ThrowsOnNullPath()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var firestore = new WasmFirestore(jsInterop);

        Assert.Throws<ArgumentNullException>(() => firestore.Collection<TestDocument>(null!));
    }

    [Fact]
    public void Collection_ThrowsOnEmptyPath()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var firestore = new WasmFirestore(jsInterop);

        Assert.Throws<ArgumentException>(() => firestore.Collection<TestDocument>(""));
    }

    [Fact]
    public async Task BatchAsync_WithEmptyBatch_ReturnsSuccess()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var firestore = new WasmFirestore(jsInterop);

        var result = await firestore.BatchAsync(batch =>
        {
            // Empty batch - no operations
        });

        // Empty batch should succeed immediately without calling JS
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task TransactionAsync_WithEmptyTransaction_ReturnsSuccess()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var firestore = new WasmFirestore(jsInterop);

        var result = await firestore.TransactionAsync<int>(async _ =>
        {
            await Task.CompletedTask;
            return 42;
        });

        // Empty transaction should succeed immediately without calling JS
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Constructor_ThrowsOnNullJsInterop()
    {
        Assert.Throws<ArgumentNullException>(() => new WasmFirestore(null!));
    }
}

public class TestDocument
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public int Age { get; set; }
    public string? Status { get; set; }
    public List<string> Tags { get; set; } = [];
}
