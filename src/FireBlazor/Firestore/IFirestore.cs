namespace FireBlazor;

/// <summary>Cloud Firestore service interface.</summary>
public interface IFirestore
{
    ICollectionReference<T> Collection<T>(string path) where T : class;
    Task<Result<Unit>> BatchAsync(Action<IWriteBatch> operations);
    Task<Result<T>> TransactionAsync<T>(Func<ITransaction, Task<T>> operations);
}

public interface ICollectionReference<T> where T : class
{
    ICollectionReference<T> Where(System.Linq.Expressions.Expression<Func<T, bool>> predicate);
    ICollectionReference<T> OrderBy<TKey>(System.Linq.Expressions.Expression<Func<T, TKey>> keySelector);
    ICollectionReference<T> OrderByDescending<TKey>(System.Linq.Expressions.Expression<Func<T, TKey>> keySelector);
    ICollectionReference<T> Take(int count);
    ICollectionReference<T> Skip(int count);
    IDocumentReference<T> Document(string id);
    Task<Result<IReadOnlyList<DocumentSnapshot<T>>>> GetAsync();
    Task<Result<DocumentReference>> AddAsync(T data);
    Action OnSnapshot(Action<IReadOnlyList<DocumentSnapshot<T>>> onNext, Action<Exception>? onError = null);

    /// <summary>
    /// Returns an aggregate query for this collection/query.
    /// </summary>
    IAggregateQuery Aggregate();

    /// <summary>
    /// Creates a query starting at the provided field values.
    /// </summary>
    ICollectionReference<T> StartAt(params object[] fieldValues);

    /// <summary>
    /// Creates a query starting after the provided field values.
    /// </summary>
    ICollectionReference<T> StartAfter(params object[] fieldValues);

    /// <summary>
    /// Creates a query ending at the provided field values.
    /// </summary>
    ICollectionReference<T> EndAt(params object[] fieldValues);

    /// <summary>
    /// Creates a query ending before the provided field values.
    /// </summary>
    ICollectionReference<T> EndBefore(params object[] fieldValues);
}

public interface IDocumentReference<T> where T : class
{
    string Id { get; }
    string Path { get; }
    ICollectionReference<TChild> Collection<TChild>(string path) where TChild : class;
    Task<Result<DocumentSnapshot<T>>> GetAsync();
    Task<Result<Unit>> SetAsync(T data, bool merge = false);
    Task<Result<Unit>> UpdateAsync(object fields);
    Task<Result<Unit>> DeleteAsync();
    Action OnSnapshot(Action<DocumentSnapshot<T>?> onNext, Action<Exception>? onError = null);
}

public sealed class DocumentSnapshot<T> where T : class
{
    public required string Id { get; init; }
    public required string Path { get; init; }
    public required bool Exists { get; init; }
    public T? Data { get; init; }
    public SnapshotMetadata? Metadata { get; init; }
}

public sealed class SnapshotMetadata
{
    public bool IsFromCache { get; init; }
    public bool HasPendingWrites { get; init; }
}

public sealed class DocumentReference
{
    public required string Id { get; init; }
    public required string Path { get; init; }
}

public interface IWriteBatch
{
    IWriteBatch Set<T>(IDocumentReference<T> doc, T data) where T : class;
    IWriteBatch Update<T>(IDocumentReference<T> doc, object fields) where T : class;
    IWriteBatch Delete<T>(IDocumentReference<T> doc) where T : class;
}

public interface ITransaction
{
    Task<Result<DocumentSnapshot<T>>> GetAsync<T>(IDocumentReference<T> doc) where T : class;
    ITransaction Set<T>(IDocumentReference<T> doc, T data) where T : class;
    ITransaction Update<T>(IDocumentReference<T> doc, object fields) where T : class;
    ITransaction Delete<T>(IDocumentReference<T> doc) where T : class;
}

/// <summary>
/// Provides aggregate query operations on a collection.
/// </summary>
public interface IAggregateQuery
{
    /// <summary>
    /// Returns the count of documents matching the query.
    /// </summary>
    Task<Result<long>> CountAsync();

    /// <summary>
    /// Returns the sum of a numeric field across matching documents.
    /// </summary>
    /// <param name="field">The name of the numeric field to sum.</param>
    Task<Result<double>> SumAsync(string field);

    /// <summary>
    /// Returns the average of a numeric field across matching documents.
    /// Returns null if no documents match.
    /// </summary>
    /// <param name="field">The name of the numeric field to average.</param>
    Task<Result<double?>> AverageAsync(string field);
}
