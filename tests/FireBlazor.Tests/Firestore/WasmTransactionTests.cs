using System.Text.Json;
using FireBlazor.Platform.Wasm;
using Microsoft.JSInterop;
using NSubstitute;

namespace FireBlazor.Tests.Firestore;

public class WasmTransactionTests
{
    public class Account
    {
        public string? Name { get; set; }
        public decimal Balance { get; set; }
    }

    [Fact]
    public void CollectingTransaction_GetAsync_CollectsReadPaths()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var firestore = new WasmFirestore(jsInterop);
        var tx = new CollectingTransaction();
        var doc = firestore.Collection<Account>("accounts").Document("1");

        tx.GetAsync(doc);

        Assert.Single(tx.ReadPaths);
        Assert.Equal("accounts/1", tx.ReadPaths[0]);
    }

    [Fact]
    public void CollectingTransaction_MultipleGetAsync_CollectsAllPaths()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var firestore = new WasmFirestore(jsInterop);
        var tx = new CollectingTransaction();
        var doc1 = firestore.Collection<Account>("accounts").Document("1");
        var doc2 = firestore.Collection<Account>("accounts").Document("2");

        tx.GetAsync(doc1);
        tx.GetAsync(doc2);

        Assert.Equal(2, tx.ReadPaths.Count);
    }

    [Fact]
    public void DirectTransaction_Set_ReturnsSameTransactionForChaining()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var firestore = new WasmFirestore(jsInterop);
        var tx = new DirectTransaction();
        var doc = firestore.Collection<Account>("accounts").Document("1");

        var returned = tx.Set(doc, new Account { Name = "Test", Balance = 100 });

        Assert.Same(tx, returned);
    }

    [Fact]
    public void DirectTransaction_Update_ReturnsSameTransactionForChaining()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var firestore = new WasmFirestore(jsInterop);
        var tx = new DirectTransaction();
        var doc = firestore.Collection<Account>("accounts").Document("1");

        var returned = tx.Update(doc, new { Balance = 200m });

        Assert.Same(tx, returned);
    }

    [Fact]
    public void DirectTransaction_Delete_ReturnsSameTransactionForChaining()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var firestore = new WasmFirestore(jsInterop);
        var tx = new DirectTransaction();
        var doc = firestore.Collection<Account>("accounts").Document("1");

        var returned = tx.Delete(doc);

        Assert.Same(tx, returned);
    }

    [Fact]
    public void DirectTransaction_MultipleOperations_CollectsAll()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var firestore = new WasmFirestore(jsInterop);
        var tx = new DirectTransaction();
        var doc1 = firestore.Collection<Account>("accounts").Document("1");
        var doc2 = firestore.Collection<Account>("accounts").Document("2");

        tx.Set(doc1, new Account { Name = "A" })
          .Update(doc2, new { Balance = 50m })
          .Delete(doc1);

        Assert.Equal(3, tx.Operations.Count);
    }

    [Fact]
    public void DirectTransaction_Operations_HaveCorrectContent()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var firestore = new WasmFirestore(jsInterop);
        var tx = new DirectTransaction();
        var doc = firestore.Collection<Account>("accounts").Document("test-id");

        tx.Set(doc, new Account { Name = "Alice", Balance = 100 });

        var op = tx.Operations[0];
        Assert.Equal("set", op.Type);
        Assert.Equal("accounts/test-id", op.Path);
        Assert.NotNull(op.Data);
    }

    [Fact]
    public void ExecutingTransaction_GetAsync_ReturnsDataFromReadData()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var firestore = new WasmFirestore(jsInterop);

        var readData = new Dictionary<string, JsonElement>
        {
            ["accounts/1"] = JsonDocument.Parse("""
                {
                    "exists": true,
                    "data": { "name": "Alice", "balance": 100 }
                }
                """).RootElement
        };

        var tx = new ExecutingTransaction(readData);
        var doc = firestore.Collection<Account>("accounts").Document("1");

        var result = tx.GetAsync(doc).Result;

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Exists);
        Assert.Equal("Alice", result.Value.Data?.Name);
        Assert.Equal(100, result.Value.Data?.Balance);
    }

    [Fact]
    public void ExecutingTransaction_GetAsync_ReturnsErrorForMissingPath()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var firestore = new WasmFirestore(jsInterop);

        var readData = new Dictionary<string, JsonElement>();
        var tx = new ExecutingTransaction(readData);
        var doc = firestore.Collection<Account>("accounts").Document("missing");

        var result = tx.GetAsync(doc).Result;

        Assert.False(result.IsSuccess);
        Assert.Contains("not pre-read", result.Error?.Message);
    }

    [Fact]
    public void ExecutingTransaction_Set_CollectsOperation()
    {
        var readData = new Dictionary<string, JsonElement>();
        var tx = new ExecutingTransaction(readData);
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var firestore = new WasmFirestore(jsInterop);
        var doc = firestore.Collection<Account>("accounts").Document("1");

        tx.Set(doc, new Account { Name = "Test" });

        Assert.Single(tx.Operations);
        Assert.Equal("set", tx.Operations[0].Type);
    }
}
