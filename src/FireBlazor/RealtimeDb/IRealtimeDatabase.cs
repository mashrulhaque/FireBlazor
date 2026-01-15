namespace FireBlazor;

/// <summary>Realtime Database service interface.</summary>
public interface IRealtimeDatabase
{
    IDatabaseReference Ref(string path);

    /// <summary>
    /// Subscribes to connection state changes.
    /// </summary>
    /// <param name="onConnected">Callback invoked when connection state changes. True = connected, False = disconnected.</param>
    /// <returns>An action to unsubscribe from connection state changes.</returns>
    Action OnConnectionStateChanged(Action<bool> onConnected);

    /// <summary>
    /// Manually disconnects from the Firebase Realtime Database server.
    /// </summary>
    Task GoOfflineAsync();

    /// <summary>
    /// Manually reconnects to the Firebase Realtime Database server after calling GoOfflineAsync.
    /// </summary>
    Task GoOnlineAsync();
}

public interface IDatabaseReference
{
    string Key { get; }
    string Path { get; }
    IDatabaseReference Child(string path);
    IDatabaseReference? Parent { get; }
    IDatabaseReference OrderByChild(string path);
    IDatabaseReference OrderByKey();
    IDatabaseReference OrderByValue();
    IDatabaseReference LimitToFirst(int count);
    IDatabaseReference LimitToLast(int count);
    IDatabaseReference StartAt(object value, string? key = null);
    IDatabaseReference EndAt(object value, string? key = null);
    IDatabaseReference EqualTo(object value, string? key = null);
    Task<Result<DataSnapshot<T>>> GetAsync<T>();
    Task<Result<Unit>> SetAsync<T>(T value);
    Task<Result<Unit>> UpdateAsync(object value);
    Task<Result<PushResult>> PushAsync<T>(T value);
    Task<Result<Unit>> RemoveAsync();

    /// <summary>
    /// Performs an atomic read-modify-write transaction at this location.
    /// </summary>
    /// <typeparam name="T">The type of data at this location.</typeparam>
    /// <param name="updateFunction">A function that receives the current value and returns the new value.
    /// Return null to abort the transaction.</param>
    /// <returns>The result containing the committed value, or an error if the transaction failed.</returns>
    Task<Result<TransactionResult<T>>> TransactionAsync<T>(Func<T?, T?> updateFunction);

    /// <summary>
    /// Provides access to onDisconnect operations for this reference.
    /// </summary>
    IOnDisconnect OnDisconnect();

    Action OnValue<T>(Action<DataSnapshot<T>> onNext, Action<Exception>? onError = null);
    Action OnChildAdded<T>(Action<DataSnapshot<T>> onNext, Action<Exception>? onError = null);
    Action OnChildChanged<T>(Action<DataSnapshot<T>> onNext, Action<Exception>? onError = null);
    Action OnChildRemoved<T>(Action<DataSnapshot<T>> onNext, Action<Exception>? onError = null);
    Action OnChildMoved<T>(Action<DataSnapshot<T>> onNext, Action<Exception>? onError = null);
}

public sealed class DataSnapshot<T>
{
    public required string Key { get; init; }
    public required bool Exists { get; init; }
    public T? Value { get; init; }
    public TValue? ValueAs<TValue>() => Value is TValue v ? v : default;
}

public sealed class PushResult
{
    public required string Key { get; init; }
    public required IDatabaseReference Reference { get; init; }
}

/// <summary>
/// Result of a successful database transaction.
/// </summary>
public sealed class TransactionResult<T>
{
    /// <summary>Whether the transaction was committed (true) or aborted by returning null (false).</summary>
    public required bool Committed { get; init; }

    /// <summary>The final value after the transaction completed.</summary>
    public T? Value { get; init; }
}

/// <summary>
/// Allows writes to be queued and executed when the client disconnects.
/// </summary>
public interface IOnDisconnect
{
    /// <summary>
    /// Ensures the data at this location is set to the specified value when the client disconnects.
    /// </summary>
    Task<Result<Unit>> SetAsync<T>(T value);

    /// <summary>
    /// Ensures the data at this location is deleted when the client disconnects.
    /// </summary>
    Task<Result<Unit>> RemoveAsync();

    /// <summary>
    /// Writes multiple values to be applied when the client disconnects.
    /// </summary>
    Task<Result<Unit>> UpdateAsync(object value);

    /// <summary>
    /// Cancels all previously queued onDisconnect operations at this location.
    /// </summary>
    Task<Result<Unit>> CancelAsync();
}
