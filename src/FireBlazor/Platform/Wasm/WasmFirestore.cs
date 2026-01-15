using System.Text.Json;
using Microsoft.JSInterop;

namespace FireBlazor.Platform.Wasm;

/// <summary>
/// WebAssembly implementation of IFirestore using JavaScript interop.
/// </summary>
internal sealed class WasmFirestore : IFirestore
{
    private readonly FirebaseJsInterop _jsInterop;

    public WasmFirestore(FirebaseJsInterop jsInterop)
    {
        _jsInterop = jsInterop ?? throw new ArgumentNullException(nameof(jsInterop));
    }

    public ICollectionReference<T> Collection<T>(string path) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return new WasmCollectionReference<T>(_jsInterop, path);
    }

    public async Task<Result<Unit>> BatchAsync(Action<IWriteBatch> operations)
    {
        var batch = new WasmWriteBatch(_jsInterop);
        operations(batch);
        return await batch.CommitAsync();
    }

    public async Task<Result<T>> TransactionAsync<T>(Func<ITransaction, Task<T>> operations)
    {
        // Phase 1: Collect document paths to read (dry run with collecting transaction)
        var collectingTx = new CollectingTransaction();
        try
        {
            await operations(collectingTx);
        }
        catch
        {
            // Ignore exceptions in dry run - we just need the read paths
        }

        var readPaths = collectingTx.ReadPaths;

        // If no reads needed, just run the callback directly
        if (readPaths.Count == 0)
        {
            try
            {
                var directTx = new DirectTransaction();
                var result = await operations(directTx);

                if (directTx.Operations.Count > 0)
                {
                    var batchResult = await _jsInterop.FirestoreBatchWriteAsync(
                        directTx.Operations.Select(op => new BatchOperation
                        {
                            Type = op.Type,
                            Path = op.Path,
                            Data = op.Data,
                            Merge = op.Merge
                        }));

                    if (!batchResult.Success)
                        return Result<T>.Failure(new FirebaseError(batchResult.Error!.Code, batchResult.Error.Message));
                }

                return Result<T>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<T>.Failure(new FirebaseError("firestore/unknown", ex.Message));
            }
        }

        // Phase 2: Execute transaction with callback
        var handler = new TransactionHandler<T>(_jsInterop, operations);
        var callbackRef = DotNetObjectReference.Create(handler);

        try
        {
            var result = await _jsInterop.FirestoreRunTransactionWithCallbackAsync(readPaths, callbackRef);

            if (!result.Success)
                return Result<T>.Failure(new FirebaseError(result.Error!.Code, result.Error.Message));

            if (handler.Exception != null)
                return Result<T>.Failure(new FirebaseError("firestore/unknown", handler.Exception.Message));

            return Result<T>.Success(handler.Result!);
        }
        catch (Exception ex)
        {
            return Result<T>.Failure(new FirebaseError("firestore/unknown", ex.Message));
        }
        finally
        {
            callbackRef.Dispose();
        }
    }
}

/// <summary>
/// Transaction that only collects document paths for reads (used in dry run).
/// </summary>
internal sealed class CollectingTransaction : ITransaction
{
    private readonly List<string> _readPaths = [];

    public IReadOnlyList<string> ReadPaths => _readPaths;

    public Task<Result<DocumentSnapshot<T>>> GetAsync<T>(IDocumentReference<T> doc) where T : class
    {
        ArgumentNullException.ThrowIfNull(doc);
        _readPaths.Add(doc.Path);

        // Return placeholder - this data won't be used
        return Task.FromResult(Result<DocumentSnapshot<T>>.Success(
            new DocumentSnapshot<T>
            {
                Id = doc.Id,
                Path = doc.Path,
                Data = default,
                Exists = false,
                Metadata = new SnapshotMetadata { IsFromCache = false, HasPendingWrites = false }
            }));
    }

    public ITransaction Set<T>(IDocumentReference<T> doc, T data) where T : class => this;
    public ITransaction Update<T>(IDocumentReference<T> doc, object fields) where T : class => this;
    public ITransaction Delete<T>(IDocumentReference<T> doc) where T : class => this;
}

/// <summary>
/// Transaction for direct execution without reads (uses batch write).
/// </summary>
internal sealed class DirectTransaction : ITransaction
{
    private readonly List<TransactionOperation> _operations = [];

    public IReadOnlyList<TransactionOperation> Operations => _operations;

    public Task<Result<DocumentSnapshot<T>>> GetAsync<T>(IDocumentReference<T> doc) where T : class
    {
        throw new InvalidOperationException("GetAsync should not be called in DirectTransaction");
    }

    public ITransaction Set<T>(IDocumentReference<T> doc, T data) where T : class
    {
        ArgumentNullException.ThrowIfNull(doc);
        ArgumentNullException.ThrowIfNull(data);
        _operations.Add(new TransactionOperation { Type = "set", Path = doc.Path, Data = data, Merge = false });
        return this;
    }

    public ITransaction Update<T>(IDocumentReference<T> doc, object fields) where T : class
    {
        ArgumentNullException.ThrowIfNull(doc);
        ArgumentNullException.ThrowIfNull(fields);
        _operations.Add(new TransactionOperation { Type = "update", Path = doc.Path, Data = fields });
        return this;
    }

    public ITransaction Delete<T>(IDocumentReference<T> doc) where T : class
    {
        ArgumentNullException.ThrowIfNull(doc);
        _operations.Add(new TransactionOperation { Type = "delete", Path = doc.Path });
        return this;
    }
}

/// <summary>
/// Handles the callback from JS during transaction execution.
/// </summary>
internal sealed class TransactionHandler<T>
{
    private readonly FirebaseJsInterop _jsInterop;
    private readonly Func<ITransaction, Task<T>> _operations;

    public T? Result { get; private set; }
    public Exception? Exception { get; private set; }

    public TransactionHandler(FirebaseJsInterop jsInterop, Func<ITransaction, Task<T>> operations)
    {
        _jsInterop = jsInterop;
        _operations = operations;
    }

    [JSInvokable]
    public async Task<TransactionCallbackResult> ProcessTransaction(Dictionary<string, JsonElement> readData)
    {
        try
        {
            var executingTx = new ExecutingTransaction(readData);
            Result = await _operations(executingTx);

            // Serialize operations with FieldValueConverter
            var serializedOps = executingTx.Operations.Select(op => new
            {
                type = op.Type,
                path = op.Path,
                data = op.Data != null ? JsonSerializer.SerializeToElement(op.Data, FirestoreJsonOptions.Default) : (JsonElement?)null,
                merge = op.Merge
            }).ToList();

            return new TransactionCallbackResult
            {
                Operations = serializedOps.Cast<object>().ToList(),
                Result = Result
            };
        }
        catch (Exception ex)
        {
            Exception = ex;
            return new TransactionCallbackResult { Error = ex.Message };
        }
    }
}

/// <summary>
/// Result returned to JS from the transaction callback.
/// </summary>
internal sealed class TransactionCallbackResult
{
    public List<object> Operations { get; set; } = [];
    public object? Result { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Transaction that executes with real data from JS.
/// </summary>
internal sealed class ExecutingTransaction : ITransaction
{
    private readonly Dictionary<string, JsonElement> _readData;
    private readonly List<TransactionOperation> _operations = [];

    public IReadOnlyList<TransactionOperation> Operations => _operations;

    public ExecutingTransaction(Dictionary<string, JsonElement> readData)
    {
        _readData = readData;
    }

    public Task<Result<DocumentSnapshot<T>>> GetAsync<T>(IDocumentReference<T> doc) where T : class
    {
        ArgumentNullException.ThrowIfNull(doc);

        if (!_readData.TryGetValue(doc.Path, out var data))
        {
            return Task.FromResult(Result<DocumentSnapshot<T>>.Failure(
                new FirebaseError("firestore/not-found", $"Document {doc.Path} was not pre-read")));
        }

        var snapshot = ParseSnapshot<T>(data, doc.Id, doc.Path);
        return Task.FromResult(Result<DocumentSnapshot<T>>.Success(snapshot));
    }

    private static DocumentSnapshot<T> ParseSnapshot<T>(JsonElement item, string docId, string docPath) where T : class
    {
        var exists = item.TryGetProperty("exists", out var existsElement) && existsElement.GetBoolean();

        T? docData = default;
        if (exists && item.TryGetProperty("data", out var dataElement) && dataElement.ValueKind != JsonValueKind.Null)
        {
            docData = JsonSerializer.Deserialize<T>(dataElement.GetRawText(), FirestoreJsonOptions.Default);
        }

        return new DocumentSnapshot<T>
        {
            Id = docId,
            Path = docPath,
            Exists = exists,
            Data = docData,
            Metadata = new SnapshotMetadata { IsFromCache = false, HasPendingWrites = false }
        };
    }

    public ITransaction Set<T>(IDocumentReference<T> doc, T data) where T : class
    {
        ArgumentNullException.ThrowIfNull(doc);
        ArgumentNullException.ThrowIfNull(data);
        _operations.Add(new TransactionOperation { Type = "set", Path = doc.Path, Data = data, Merge = false });
        return this;
    }

    public ITransaction Update<T>(IDocumentReference<T> doc, object fields) where T : class
    {
        ArgumentNullException.ThrowIfNull(doc);
        ArgumentNullException.ThrowIfNull(fields);
        _operations.Add(new TransactionOperation { Type = "update", Path = doc.Path, Data = fields });
        return this;
    }

    public ITransaction Delete<T>(IDocumentReference<T> doc) where T : class
    {
        ArgumentNullException.ThrowIfNull(doc);
        _operations.Add(new TransactionOperation { Type = "delete", Path = doc.Path });
        return this;
    }
}

/// <summary>
/// WebAssembly implementation of IWriteBatch for atomic batch writes.
/// </summary>
internal sealed class WasmWriteBatch : IWriteBatch
{
    private readonly FirebaseJsInterop _jsInterop;
    private readonly List<BatchOperation> _operations = [];

    public WasmWriteBatch(FirebaseJsInterop jsInterop)
    {
        ArgumentNullException.ThrowIfNull(jsInterop);
        _jsInterop = jsInterop;
    }

    public IReadOnlyList<BatchOperation> Operations => _operations;

    public IWriteBatch Set<T>(IDocumentReference<T> doc, T data) where T : class
    {
        ArgumentNullException.ThrowIfNull(doc);
        ArgumentNullException.ThrowIfNull(data);
        _operations.Add(new BatchOperation
        {
            Type = "set",
            Path = doc.Path,
            Data = data,
            Merge = false
        });
        return this;
    }

    public IWriteBatch Update<T>(IDocumentReference<T> doc, object fields) where T : class
    {
        ArgumentNullException.ThrowIfNull(doc);
        ArgumentNullException.ThrowIfNull(fields);
        _operations.Add(new BatchOperation
        {
            Type = "update",
            Path = doc.Path,
            Data = fields
        });
        return this;
    }

    public IWriteBatch Delete<T>(IDocumentReference<T> doc) where T : class
    {
        ArgumentNullException.ThrowIfNull(doc);
        _operations.Add(new BatchOperation
        {
            Type = "delete",
            Path = doc.Path
        });
        return this;
    }

    internal async Task<Result<Unit>> CommitAsync()
    {
        if (_operations.Count == 0)
            return Unit.Value;

        if (_operations.Count > 500)
            return Result<Unit>.Failure(new FirebaseError(
                "firestore/invalid-argument",
                "Batch cannot contain more than 500 operations"));

        var result = await _jsInterop.FirestoreBatchWriteAsync(_operations);

        if (!result.Success)
            return Result<Unit>.Failure(new FirebaseError(result.Error!.Code, result.Error.Message));

        return Unit.Value;
    }
}
