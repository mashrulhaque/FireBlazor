using FireBlazor.Platform.Wasm;
using Microsoft.JSInterop;
using NSubstitute;

namespace FireBlazor.Tests.Firestore;

public class WasmWriteBatchTests
{
    public class TestDoc
    {
        public string? Name { get; set; }
        public int Count { get; set; }
    }

    [Fact]
    public void WriteBatch_Set_ReturnsSameBatchForChaining()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var firestore = new WasmFirestore(jsInterop);
        var batch = new WasmWriteBatch(jsInterop);
        var doc = firestore.Collection<TestDoc>("items").Document("1");

        var returned = batch.Set(doc, new TestDoc { Name = "Test" });

        Assert.Same(batch, returned);
    }

    [Fact]
    public void WriteBatch_Update_ReturnsSameBatchForChaining()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var firestore = new WasmFirestore(jsInterop);
        var batch = new WasmWriteBatch(jsInterop);
        var doc = firestore.Collection<TestDoc>("items").Document("1");

        var returned = batch.Update(doc, new { Name = "Updated" });

        Assert.Same(batch, returned);
    }

    [Fact]
    public void WriteBatch_Delete_ReturnsSameBatchForChaining()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var firestore = new WasmFirestore(jsInterop);
        var batch = new WasmWriteBatch(jsInterop);
        var doc = firestore.Collection<TestDoc>("items").Document("1");

        var returned = batch.Delete(doc);

        Assert.Same(batch, returned);
    }

    [Fact]
    public void WriteBatch_MultipleOperations_CollectsAll()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var firestore = new WasmFirestore(jsInterop);
        var batch = new WasmWriteBatch(jsInterop);
        var doc1 = firestore.Collection<TestDoc>("items").Document("1");
        var doc2 = firestore.Collection<TestDoc>("items").Document("2");

        batch.Set(doc1, new TestDoc { Name = "A" })
             .Update(doc2, new { Count = 5 })
             .Delete(doc1);

        // Verify 3 operations were collected
        Assert.Equal(3, batch.Operations.Count);
    }
}
