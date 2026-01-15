using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FireBlazor.Testing;

/// <summary>
/// In-memory fake implementation of IFirestore for testing.
/// </summary>
/// <remarks>
/// This fake is designed for single-threaded unit tests.
/// It is NOT thread-safe for concurrent access from multiple threads.
/// Note: Query methods (Where, OrderBy) are no-ops in this fake implementation.
/// </remarks>
public sealed class FakeFirestore : IFirestore
{
    private readonly Dictionary<string, JsonElement> _documents = new();
    private readonly Dictionary<string, List<Action<string>>> _documentListeners = new();
    private readonly Dictionary<string, List<Action<string>>> _collectionListeners = new();
    private FirebaseError? _simulatedError;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions JsonOptionsWithFieldValue = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new FieldValueConverter() }
    };

    public ICollectionReference<T> Collection<T>(string path) where T : class
    {
        return new FakeCollectionReference<T>(this, path);
    }

    public Task<Result<Unit>> BatchAsync(Action<IWriteBatch> operations)
    {
        if (TryConsumeSimulatedError(out var error))
            return Task.FromResult(Result<Unit>.Failure(error!));

        var batch = new FakeWriteBatch(this);
        operations(batch);
        return Task.FromResult(Result<Unit>.Success(Unit.Value));
    }

    public Task<Result<T>> TransactionAsync<T>(Func<ITransaction, Task<T>> operations)
    {
        if (TryConsumeSimulatedError(out var error))
            return Task.FromResult(Result<T>.Failure(error!));

        var transaction = new FakeTransaction(this);
        var result = operations(transaction).Result;
        return Task.FromResult(Result<T>.Success(result));
    }

    /// <summary>
    /// Seeds data into the fake Firestore.
    /// </summary>
    public void SeedData<T>(string collectionPath, Dictionary<string, T> documents) where T : class
    {
        foreach (var (id, data) in documents)
        {
            var path = $"{collectionPath}/{id}";
            var json = JsonSerializer.SerializeToElement(data, JsonOptions);
            _documents[path] = json;
        }
    }

    /// <summary>
    /// Seeds data into the fake Firestore using an array of documents.
    /// Documents must have an Id property to determine the document ID.
    /// </summary>
    public void SeedData<T>(string collectionPath, IEnumerable<T> documents) where T : class
    {
        foreach (var data in documents)
        {
            var idProperty = typeof(T).GetProperty("Id");
            var id = idProperty?.GetValue(data)?.ToString() ?? Guid.NewGuid().ToString();
            var path = $"{collectionPath}/{id}";
            var json = JsonSerializer.SerializeToElement(data, JsonOptions);
            _documents[path] = json;
        }
    }

    /// <summary>
    /// Simulates an error for the next operation.
    /// </summary>
    public void SimulateError(FirebaseError error)
    {
        _simulatedError = error;
    }

    /// <summary>
    /// Resets all state.
    /// </summary>
    public void Reset()
    {
        _documents.Clear();
        _documentListeners.Clear();
        _collectionListeners.Clear();
        _simulatedError = null;
    }

    internal bool TryConsumeSimulatedError(out FirebaseError? error)
    {
        error = _simulatedError;
        _simulatedError = null;
        return error != null;
    }

    internal void SetDocument(string path, object data)
    {
        var json = JsonSerializer.SerializeToElement(data, JsonOptions);
        _documents[path] = json;
        NotifyDocumentListeners(path);
        NotifyCollectionListeners(path);
    }

    internal T? GetDocument<T>(string path) where T : class
    {
        if (_documents.TryGetValue(path, out var json))
        {
            return JsonSerializer.Deserialize<T>(json.GetRawText(), JsonOptions);
        }
        return null;
    }

    internal bool DocumentExists(string path) => _documents.ContainsKey(path);

    internal void DeleteDocument(string path)
    {
        _documents.Remove(path);
        NotifyDocumentListeners(path);
        NotifyCollectionListeners(path);
    }

    internal void UpdateDocument(string path, object fields)
    {
        // Serialize the update fields with FieldValue support
        var updateJson = JsonSerializer.SerializeToElement(fields, JsonOptionsWithFieldValue);

        // Get the existing document or create empty object
        JsonObject existing;
        if (_documents.TryGetValue(path, out var existingDoc))
        {
            existing = JsonNode.Parse(existingDoc.GetRawText())?.AsObject() ?? new JsonObject();
        }
        else
        {
            existing = new JsonObject();
        }

        // Parse the update as a JsonObject
        var updateNode = JsonNode.Parse(updateJson.GetRawText())?.AsObject();
        if (updateNode == null)
        {
            return;
        }

        // Apply each field from the update, processing FieldValues
        foreach (var prop in updateNode)
        {
            var fieldName = prop.Key;
            var fieldValue = prop.Value;

            if (IsFieldValueSentinel(fieldValue, out var fieldValueType, out var data))
            {
                ApplyFieldValue(existing, fieldName, fieldValueType, data);
            }
            else
            {
                // Regular field update - replace the value
                existing[fieldName] = fieldValue?.DeepClone();
            }
        }

        // Store the merged document
        var mergedJson = JsonSerializer.SerializeToElement(existing, JsonOptions);
        _documents[path] = mergedJson;
        NotifyDocumentListeners(path);
        NotifyCollectionListeners(path);
    }

    private static bool IsFieldValueSentinel(JsonNode? node, out string type, out JsonNode? data)
    {
        type = string.Empty;
        data = null;

        if (node is not JsonObject obj)
            return false;

        if (!obj.TryGetPropertyValue("__fieldValue__", out var typeNode) || typeNode == null)
            return false;

        type = typeNode.GetValue<string>();
        data = obj;
        return true;
    }

    private static void ApplyFieldValue(JsonObject document, string fieldName, string fieldValueType, JsonNode? data)
    {
        switch (fieldValueType)
        {
            case "serverTimestamp":
                document[fieldName] = DateTime.UtcNow.ToString("O");
                break;

            case "increment":
                ApplyIncrement(document, fieldName, data);
                break;

            case "arrayUnion":
                ApplyArrayUnion(document, fieldName, data);
                break;

            case "arrayRemove":
                ApplyArrayRemove(document, fieldName, data);
                break;

            case "delete":
                document.Remove(fieldName);
                break;
        }
    }

    private static void ApplyIncrement(JsonObject document, string fieldName, JsonNode? data)
    {
        if (data is not JsonObject dataObj)
            return;

        if (!dataObj.TryGetPropertyValue("value", out var valueNode) || valueNode == null)
            return;

        var incrementBy = valueNode.GetValue<double>();

        double currentValue = 0;
        if (document.TryGetPropertyValue(fieldName, out var existingNode) && existingNode != null)
        {
            currentValue = existingNode.GetValue<double>();
        }

        document[fieldName] = currentValue + incrementBy;
    }

    private static void ApplyArrayUnion(JsonObject document, string fieldName, JsonNode? data)
    {
        if (data is not JsonObject dataObj)
            return;

        if (!dataObj.TryGetPropertyValue("elements", out var elementsNode) || elementsNode is not JsonArray elementsToAdd)
            return;

        // Get existing array or create new one
        JsonArray existingArray;
        if (document.TryGetPropertyValue(fieldName, out var existingNode) && existingNode is JsonArray arr)
        {
            existingArray = arr;
        }
        else
        {
            existingArray = new JsonArray();
            document[fieldName] = existingArray;
        }

        // Add elements that don't already exist
        foreach (var element in elementsToAdd)
        {
            var elementStr = element?.ToJsonString();
            var exists = existingArray.Any(e => e?.ToJsonString() == elementStr);
            if (!exists)
            {
                existingArray.Add(element?.DeepClone());
            }
        }
    }

    private static void ApplyArrayRemove(JsonObject document, string fieldName, JsonNode? data)
    {
        if (data is not JsonObject dataObj)
            return;

        if (!dataObj.TryGetPropertyValue("elements", out var elementsNode) || elementsNode is not JsonArray elementsToRemove)
            return;

        // Get existing array
        if (!document.TryGetPropertyValue(fieldName, out var existingNode) || existingNode is not JsonArray existingArray)
            return;

        // Build set of elements to remove
        var toRemoveSet = new HashSet<string>(
            elementsToRemove.Select(e => e?.ToJsonString() ?? ""));

        // Create new array without the elements to remove
        var newArray = new JsonArray();
        foreach (var element in existingArray)
        {
            var elementStr = element?.ToJsonString();
            if (!toRemoveSet.Contains(elementStr ?? ""))
            {
                newArray.Add(element?.DeepClone());
            }
        }

        document[fieldName] = newArray;
    }

    internal IEnumerable<(string Path, T? Data)> GetCollection<T>(string collectionPath) where T : class
    {
        var prefix = collectionPath + "/";
        foreach (var (path, json) in _documents)
        {
            if (path.StartsWith(prefix) && !path.Substring(prefix.Length).Contains('/'))
            {
                var data = JsonSerializer.Deserialize<T>(json.GetRawText(), JsonOptions);
                yield return (path, data);
            }
        }
    }

    internal List<JsonElement> GetCollectionRaw(string collectionPath)
    {
        var result = new List<JsonElement>();
        var prefix = collectionPath + "/";
        foreach (var (path, json) in _documents)
        {
            if (path.StartsWith(prefix) && !path.Substring(prefix.Length).Contains('/'))
            {
                result.Add(json);
            }
        }
        return result;
    }

    internal void SubscribeToDocument(string path, Action<string> callback)
    {
        if (!_documentListeners.ContainsKey(path))
            _documentListeners[path] = [];
        _documentListeners[path].Add(callback);
    }

    internal void UnsubscribeFromDocument(string path, Action<string> callback)
    {
        if (_documentListeners.TryGetValue(path, out var listeners))
            listeners.Remove(callback);
    }

    internal void SubscribeToCollection(string path, Action<string> callback)
    {
        if (!_collectionListeners.ContainsKey(path))
            _collectionListeners[path] = [];
        _collectionListeners[path].Add(callback);
    }

    internal void UnsubscribeFromCollection(string path, Action<string> callback)
    {
        if (_collectionListeners.TryGetValue(path, out var listeners))
            listeners.Remove(callback);
    }

    private void NotifyDocumentListeners(string path)
    {
        if (_documentListeners.TryGetValue(path, out var listeners))
        {
            foreach (var listener in listeners.ToList())
                listener(path);
        }
    }

    private void NotifyCollectionListeners(string documentPath)
    {
        var collectionPath = documentPath.Contains('/')
            ? documentPath.Substring(0, documentPath.LastIndexOf('/'))
            : documentPath;

        if (_collectionListeners.TryGetValue(collectionPath, out var listeners))
        {
            foreach (var listener in listeners.ToList())
                listener(collectionPath);
        }
    }
}

/// <summary>
/// Fake implementation of ICollectionReference for testing.
/// </summary>
/// <remarks>
/// <para>
/// This implementation supports Where, OrderBy, Take, Skip, and cursor pagination methods.
/// </para>
/// <para>
/// <b>Cursor Limitation:</b> When using multiple OrderBy fields with cursor methods
/// (StartAt, StartAfter, EndAt, EndBefore), only the first OrderBy field is used for
/// cursor comparison. This is a simplification for testing purposes. The actual
/// Firebase Firestore implementation correctly handles multi-field cursors.
/// </para>
/// </remarks>
/// <typeparam name="T">The document type.</typeparam>
internal sealed class FakeCollectionReference<T> : ICollectionReference<T> where T : class
{
    private readonly FakeFirestore _firestore;
    private readonly string _path;
    private readonly List<Func<T, bool>> _wherePredicates = [];
    private readonly List<(string Field, bool Descending)> _orderByFields = [];
    private int? _limit;
    private int? _skip;
    private object[]? _startAt;
    private object[]? _startAfter;
    private object[]? _endAt;
    private object[]? _endBefore;

    public FakeCollectionReference(FakeFirestore firestore, string path)
    {
        _firestore = firestore;
        _path = path;
    }

    private FakeCollectionReference(
        FakeFirestore firestore,
        string path,
        List<Func<T, bool>> wherePredicates,
        List<(string Field, bool Descending)> orderByFields,
        int? limit,
        int? skip,
        object[]? startAt,
        object[]? startAfter,
        object[]? endAt,
        object[]? endBefore)
    {
        _firestore = firestore;
        _path = path;
        _wherePredicates = [.. wherePredicates];
        _orderByFields = [.. orderByFields];
        _limit = limit;
        _skip = skip;
        _startAt = startAt;
        _startAfter = startAfter;
        _endAt = endAt;
        _endBefore = endBefore;
    }

    public ICollectionReference<T> Where(Expression<Func<T, bool>> predicate)
    {
        var compiled = predicate.Compile();
        return new FakeCollectionReference<T>(_firestore, _path, [.. _wherePredicates, compiled], _orderByFields,
            _limit, _skip, _startAt, _startAfter, _endAt, _endBefore);
    }

    public ICollectionReference<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        var memberName = GetMemberName(keySelector);
        return new FakeCollectionReference<T>(_firestore, _path, _wherePredicates, [.. _orderByFields, (memberName, false)],
            _limit, _skip, _startAt, _startAfter, _endAt, _endBefore);
    }

    public ICollectionReference<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        var memberName = GetMemberName(keySelector);
        return new FakeCollectionReference<T>(_firestore, _path, _wherePredicates, [.. _orderByFields, (memberName, true)],
            _limit, _skip, _startAt, _startAfter, _endAt, _endBefore);
    }

    public ICollectionReference<T> Take(int count)
    {
        return new FakeCollectionReference<T>(_firestore, _path, _wherePredicates, _orderByFields,
            count, _skip, _startAt, _startAfter, _endAt, _endBefore);
    }

    public ICollectionReference<T> Skip(int count)
    {
        return new FakeCollectionReference<T>(_firestore, _path, _wherePredicates, _orderByFields,
            _limit, count, _startAt, _startAfter, _endAt, _endBefore);
    }

    public IDocumentReference<T> Document(string id)
    {
        return new FakeDocumentReference<T>(_firestore, $"{_path}/{id}");
    }

    public Task<Result<IReadOnlyList<DocumentSnapshot<T>>>> GetAsync()
    {
        // Validate: cursors require orderBy (Firestore requirement)
        var hasCursors = _startAt != null || _startAfter != null || _endAt != null || _endBefore != null;
        if (hasCursors && _orderByFields.Count == 0)
        {
            throw new InvalidOperationException(
                "Cursor methods (StartAt, StartAfter, EndAt, EndBefore) require OrderBy to be specified first. " +
                "Firestore cursors are based on field values in the order specified by OrderBy clauses.");
        }

        if (_firestore.TryConsumeSimulatedError(out var error))
            return Task.FromResult(Result<IReadOnlyList<DocumentSnapshot<T>>>.Failure(error!));

        IEnumerable<(string Path, T? Data)> docs = _firestore.GetCollection<T>(_path);

        // Apply where predicates
        foreach (var predicate in _wherePredicates)
        {
            docs = docs.Where(d => d.Data != null && predicate(d.Data));
        }

        // Convert to list for sorting
        var docList = docs.Where(d => d.Data != null).ToList();

        // Apply ordering
        if (_orderByFields.Count > 0)
        {
            docList = ApplyOrdering(docList);
        }

        // Apply cursor-based filtering
        docList = ApplyCursors(docList);

        // Apply skip and limit
        IEnumerable<(string Path, T? Data)> result = docList;
        if (_skip.HasValue)
            result = result.Skip(_skip.Value);
        if (_limit.HasValue)
            result = result.Take(_limit.Value);

        var snapshots = result.Select(d => new DocumentSnapshot<T>
        {
            Id = d.Path.Split('/').Last(),
            Path = d.Path,
            Exists = true,
            Data = d.Data,
            Metadata = new SnapshotMetadata { IsFromCache = false, HasPendingWrites = false }
        }).ToList();

        return Task.FromResult(Result<IReadOnlyList<DocumentSnapshot<T>>>.Success(snapshots));
    }

    private List<(string Path, T? Data)> ApplyOrdering(List<(string Path, T? Data)> docs)
    {
        if (_orderByFields.Count == 0)
            return docs;

        IOrderedEnumerable<(string Path, T? Data)>? ordered = null;

        foreach (var (field, descending) in _orderByFields)
        {
            var prop = typeof(T).GetProperty(field);
            if (prop == null) continue;

            if (ordered == null)
            {
                ordered = descending
                    ? docs.OrderByDescending(d => prop.GetValue(d.Data))
                    : docs.OrderBy(d => prop.GetValue(d.Data));
            }
            else
            {
                ordered = descending
                    ? ordered.ThenByDescending(d => prop.GetValue(d.Data))
                    : ordered.ThenBy(d => prop.GetValue(d.Data));
            }
        }

        return ordered?.ToList() ?? docs;
    }

    private List<(string Path, T? Data)> ApplyCursors(List<(string Path, T? Data)> docs)
    {
        if (_orderByFields.Count == 0)
            return docs;

        // Get the first orderBy field for cursor comparison
        var (orderField, descending) = _orderByFields[0];
        var prop = typeof(T).GetProperty(orderField);
        if (prop == null)
            return docs;

        var result = new List<(string Path, T? Data)>();

        foreach (var doc in docs)
        {
            if (doc.Data == null) continue;

            var value = prop.GetValue(doc.Data);
            var comparable = value as IComparable;

            bool passesStartAt = true;
            bool passesStartAfter = true;
            bool passesEndAt = true;
            bool passesEndBefore = true;

            if (_startAt != null && _startAt.Length > 0)
            {
                var cursorValue = _startAt[0] as IComparable;
                if (comparable != null && cursorValue != null)
                {
                    var comparison = comparable.CompareTo(cursorValue);
                    passesStartAt = descending ? comparison <= 0 : comparison >= 0;
                }
            }

            if (_startAfter != null && _startAfter.Length > 0)
            {
                var cursorValue = _startAfter[0] as IComparable;
                if (comparable != null && cursorValue != null)
                {
                    var comparison = comparable.CompareTo(cursorValue);
                    passesStartAfter = descending ? comparison < 0 : comparison > 0;
                }
            }

            if (_endAt != null && _endAt.Length > 0)
            {
                var cursorValue = _endAt[0] as IComparable;
                if (comparable != null && cursorValue != null)
                {
                    var comparison = comparable.CompareTo(cursorValue);
                    passesEndAt = descending ? comparison >= 0 : comparison <= 0;
                }
            }

            if (_endBefore != null && _endBefore.Length > 0)
            {
                var cursorValue = _endBefore[0] as IComparable;
                if (comparable != null && cursorValue != null)
                {
                    var comparison = comparable.CompareTo(cursorValue);
                    passesEndBefore = descending ? comparison > 0 : comparison < 0;
                }
            }

            if (passesStartAt && passesStartAfter && passesEndAt && passesEndBefore)
            {
                result.Add(doc);
            }
        }

        return result;
    }

    public Task<Result<DocumentReference>> AddAsync(T data)
    {
        if (_firestore.TryConsumeSimulatedError(out var error))
            return Task.FromResult(Result<DocumentReference>.Failure(error!));

        var id = Guid.NewGuid().ToString();
        var path = $"{_path}/{id}";
        _firestore.SetDocument(path, data);
        return Task.FromResult(Result<DocumentReference>.Success(new DocumentReference { Id = id, Path = path }));
    }

    public Action OnSnapshot(Action<IReadOnlyList<DocumentSnapshot<T>>> onNext, Action<Exception>? onError = null)
    {
        void Handler(string _) => onNext(GetAsync().Result.Value);

        _firestore.SubscribeToCollection(_path, Handler);

        // Emit initial snapshot
        onNext(GetAsync().Result.Value);

        return () => _firestore.UnsubscribeFromCollection(_path, Handler);
    }

    public IAggregateQuery Aggregate()
    {
        return new FakeAggregateQuery<T>(_firestore, _path);
    }

    public ICollectionReference<T> StartAt(params object[] fieldValues)
    {
        ArgumentNullException.ThrowIfNull(fieldValues);
        if (fieldValues.Length == 0)
            throw new ArgumentException("At least one field value is required", nameof(fieldValues));

        return new FakeCollectionReference<T>(_firestore, _path, _wherePredicates, _orderByFields,
            _limit, _skip, fieldValues, _startAfter, _endAt, _endBefore);
    }

    public ICollectionReference<T> StartAfter(params object[] fieldValues)
    {
        ArgumentNullException.ThrowIfNull(fieldValues);
        if (fieldValues.Length == 0)
            throw new ArgumentException("At least one field value is required", nameof(fieldValues));

        return new FakeCollectionReference<T>(_firestore, _path, _wherePredicates, _orderByFields,
            _limit, _skip, _startAt, fieldValues, _endAt, _endBefore);
    }

    public ICollectionReference<T> EndAt(params object[] fieldValues)
    {
        ArgumentNullException.ThrowIfNull(fieldValues);
        if (fieldValues.Length == 0)
            throw new ArgumentException("At least one field value is required", nameof(fieldValues));

        return new FakeCollectionReference<T>(_firestore, _path, _wherePredicates, _orderByFields,
            _limit, _skip, _startAt, _startAfter, fieldValues, _endBefore);
    }

    public ICollectionReference<T> EndBefore(params object[] fieldValues)
    {
        ArgumentNullException.ThrowIfNull(fieldValues);
        if (fieldValues.Length == 0)
            throw new ArgumentException("At least one field value is required", nameof(fieldValues));

        return new FakeCollectionReference<T>(_firestore, _path, _wherePredicates, _orderByFields,
            _limit, _skip, _startAt, _startAfter, _endAt, fieldValues);
    }

    private static string GetMemberName<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        if (keySelector.Body is MemberExpression memberExpr)
            return memberExpr.Member.Name;

        throw new ArgumentException("Expression must be a member access expression", nameof(keySelector));
    }
}

internal sealed class FakeDocumentReference<T> : IDocumentReference<T> where T : class
{
    private readonly FakeFirestore _firestore;
    private readonly string _path;

    public FakeDocumentReference(FakeFirestore firestore, string path)
    {
        _firestore = firestore;
        _path = path;
    }

    public string Id => _path.Split('/').Last();
    public string Path => _path;

    public ICollectionReference<TChild> Collection<TChild>(string path) where TChild : class
    {
        return new FakeCollectionReference<TChild>(_firestore, $"{_path}/{path}");
    }

    public Task<Result<DocumentSnapshot<T>>> GetAsync()
    {
        if (_firestore.TryConsumeSimulatedError(out var error))
            return Task.FromResult(Result<DocumentSnapshot<T>>.Failure(error!));

        var data = _firestore.GetDocument<T>(_path);
        var exists = _firestore.DocumentExists(_path);

        return Task.FromResult(Result<DocumentSnapshot<T>>.Success(new DocumentSnapshot<T>
        {
            Id = Id,
            Path = _path,
            Exists = exists,
            Data = data,
            Metadata = new SnapshotMetadata { IsFromCache = false, HasPendingWrites = false }
        }));
    }

    public Task<Result<Unit>> SetAsync(T data, bool merge = false)
    {
        if (_firestore.TryConsumeSimulatedError(out var error))
            return Task.FromResult(Result<Unit>.Failure(error!));

        _firestore.SetDocument(_path, data);
        return Task.FromResult(Result<Unit>.Success(Unit.Value));
    }

    public Task<Result<Unit>> UpdateAsync(object fields)
    {
        if (_firestore.TryConsumeSimulatedError(out var error))
            return Task.FromResult(Result<Unit>.Failure(error!));

        _firestore.UpdateDocument(_path, fields);
        return Task.FromResult(Result<Unit>.Success(Unit.Value));
    }

    public Task<Result<Unit>> DeleteAsync()
    {
        if (_firestore.TryConsumeSimulatedError(out var error))
            return Task.FromResult(Result<Unit>.Failure(error!));

        _firestore.DeleteDocument(_path);
        return Task.FromResult(Result<Unit>.Success(Unit.Value));
    }

    public Action OnSnapshot(Action<DocumentSnapshot<T>?> onNext, Action<Exception>? onError = null)
    {
        void Handler(string _) => onNext(GetAsync().Result.Value);

        _firestore.SubscribeToDocument(_path, Handler);

        // Emit initial snapshot
        var snapshot = GetAsync().Result.Value;
        onNext(snapshot.Exists ? snapshot : null);

        return () => _firestore.UnsubscribeFromDocument(_path, Handler);
    }
}

internal sealed class FakeWriteBatch : IWriteBatch
{
    private readonly FakeFirestore _firestore;

    public FakeWriteBatch(FakeFirestore firestore) => _firestore = firestore;

    public IWriteBatch Set<T>(IDocumentReference<T> doc, T data) where T : class
    {
        _firestore.SetDocument(doc.Path, data!);
        return this;
    }

    public IWriteBatch Update<T>(IDocumentReference<T> doc, object fields) where T : class
    {
        _firestore.UpdateDocument(doc.Path, fields);
        return this;
    }

    public IWriteBatch Delete<T>(IDocumentReference<T> doc) where T : class
    {
        _firestore.DeleteDocument(doc.Path);
        return this;
    }
}

internal sealed class FakeTransaction : ITransaction
{
    private readonly FakeFirestore _firestore;

    public FakeTransaction(FakeFirestore firestore) => _firestore = firestore;

    public Task<Result<DocumentSnapshot<T>>> GetAsync<T>(IDocumentReference<T> doc) where T : class
    {
        return doc.GetAsync();
    }

    public ITransaction Set<T>(IDocumentReference<T> doc, T data) where T : class
    {
        _firestore.SetDocument(doc.Path, data!);
        return this;
    }

    public ITransaction Update<T>(IDocumentReference<T> doc, object fields) where T : class
    {
        _firestore.UpdateDocument(doc.Path, fields);
        return this;
    }

    public ITransaction Delete<T>(IDocumentReference<T> doc) where T : class
    {
        _firestore.DeleteDocument(doc.Path);
        return this;
    }
}

internal sealed class FakeAggregateQuery<T> : IAggregateQuery where T : class
{
    private readonly FakeFirestore _firestore;
    private readonly string _path;

    public FakeAggregateQuery(FakeFirestore firestore, string path)
    {
        _firestore = firestore;
        _path = path;
    }

    public Task<Result<long>> CountAsync()
    {
        if (_firestore.TryConsumeSimulatedError(out var error))
            return Task.FromResult(Result<long>.Failure(error!));

        var docs = _firestore.GetCollectionRaw(_path);
        return Task.FromResult(Result<long>.Success(docs.Count));
    }

    public Task<Result<double>> SumAsync(string field)
    {
        if (_firestore.TryConsumeSimulatedError(out var error))
            return Task.FromResult(Result<double>.Failure(error!));

        var docs = _firestore.GetCollectionRaw(_path);
        var fieldName = ToCamelCase(field);

        double sum = 0;
        foreach (var doc in docs)
        {
            if (doc.TryGetProperty(fieldName, out var value) && value.ValueKind == JsonValueKind.Number)
            {
                sum += value.GetDouble();
            }
        }

        return Task.FromResult(Result<double>.Success(sum));
    }

    public Task<Result<double?>> AverageAsync(string field)
    {
        if (_firestore.TryConsumeSimulatedError(out var error))
            return Task.FromResult(Result<double?>.Failure(error!));

        var docs = _firestore.GetCollectionRaw(_path);
        if (docs.Count == 0)
            return Task.FromResult(Result<double?>.Success(null));

        var fieldName = ToCamelCase(field);
        double sum = 0;
        int count = 0;

        foreach (var doc in docs)
        {
            if (doc.TryGetProperty(fieldName, out var value) && value.ValueKind == JsonValueKind.Number)
            {
                sum += value.GetDouble();
                count++;
            }
        }

        if (count == 0)
            return Task.FromResult(Result<double?>.Success(null));

        return Task.FromResult(Result<double?>.Success(sum / count));
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        return char.ToLowerInvariant(name[0]) + name[1..];
    }
}
