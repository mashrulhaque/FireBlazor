using System.Text.Json;

namespace FireBlazor.Testing;

/// <summary>
/// In-memory fake implementation of IRealtimeDatabase for testing.
/// </summary>
/// <remarks>
/// This fake is designed for single-threaded unit tests.
/// It is NOT thread-safe for concurrent access from multiple threads.
/// Note: Query methods (OrderByChild, LimitToFirst, etc.) are no-ops in this fake implementation.
/// </remarks>
public sealed class FakeRealtimeDatabase : IRealtimeDatabase
{
    private readonly Dictionary<string, JsonElement> _data = new();
    private readonly Dictionary<string, List<Action<string>>> _valueListeners = new();
    private readonly Dictionary<string, List<Action<string>>> _childListeners = new();
    private readonly List<Action<bool>> _connectionListeners = new();
    private FirebaseError? _simulatedError;
    private int _pushKeyCounter;
    private bool _isConnected = true;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public IDatabaseReference Ref(string path)
    {
        return new FakeDatabaseReference(this, NormalizePath(path));
    }

    /// <summary>
    /// Seeds data at a specific path.
    /// </summary>
    public void SeedData<T>(string path, T value)
    {
        var json = JsonSerializer.SerializeToElement(value, JsonOptions);
        _data[NormalizePath(path)] = json;
    }

    /// <summary>
    /// Simulates an error for the next operation.
    /// </summary>
    public void SimulateError(FirebaseError error)
    {
        _simulatedError = error;
    }

    /// <summary>
    /// Simulates a connection state change for testing.
    /// </summary>
    public void SimulateConnectionState(bool isConnected)
    {
        _isConnected = isConnected;
        foreach (var listener in _connectionListeners.ToList())
        {
            listener(isConnected);
        }
    }

    public Action OnConnectionStateChanged(Action<bool> onConnected)
    {
        _connectionListeners.Add(onConnected);
        // Immediately notify current state
        onConnected(_isConnected);
        return () => _connectionListeners.Remove(onConnected);
    }

    public Task GoOfflineAsync()
    {
        SimulateConnectionState(false);
        return Task.CompletedTask;
    }

    public Task GoOnlineAsync()
    {
        SimulateConnectionState(true);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Resets all state.
    /// </summary>
    public void Reset()
    {
        _data.Clear();
        _valueListeners.Clear();
        _childListeners.Clear();
        _connectionListeners.Clear();
        _simulatedError = null;
        _pushKeyCounter = 0;
        _isConnected = true;
    }

    internal bool TryConsumeSimulatedError(out FirebaseError? error)
    {
        error = _simulatedError;
        _simulatedError = null;
        return error != null;
    }

    internal void SetValue(string path, object value)
    {
        var json = JsonSerializer.SerializeToElement(value, JsonOptions);
        _data[path] = json;
        NotifyValueListeners(path);
    }

    internal T? GetValue<T>(string path)
    {
        if (_data.TryGetValue(path, out var json))
        {
            return JsonSerializer.Deserialize<T>(json.GetRawText(), JsonOptions);
        }
        return default;
    }

    internal bool HasValue(string path) => _data.ContainsKey(path);

    internal void RemoveValue(string path)
    {
        _data.Remove(path);
        NotifyValueListeners(path);
    }

    internal string GeneratePushKey()
    {
        return $"-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{_pushKeyCounter++:D6}";
    }

    internal void SubscribeToValue(string path, Action<string> callback)
    {
        if (!_valueListeners.ContainsKey(path))
            _valueListeners[path] = [];
        _valueListeners[path].Add(callback);
    }

    internal void UnsubscribeFromValue(string path, Action<string> callback)
    {
        if (_valueListeners.TryGetValue(path, out var listeners))
            listeners.Remove(callback);
    }

    private void NotifyValueListeners(string path)
    {
        // Notify exact path
        if (_valueListeners.TryGetValue(path, out var listeners))
        {
            foreach (var listener in listeners.ToList())
                listener(path);
        }

        // Notify parent paths
        var parts = path.Split('/');
        for (int i = parts.Length - 1; i > 0; i--)
        {
            var parentPath = string.Join('/', parts.Take(i));
            if (_valueListeners.TryGetValue(parentPath, out var parentListeners))
            {
                foreach (var listener in parentListeners.ToList())
                    listener(parentPath);
            }
        }
    }

    private static string NormalizePath(string path)
    {
        return path.TrimStart('/').TrimEnd('/');
    }
}

internal sealed class FakeDatabaseReference : IDatabaseReference
{
    private readonly FakeRealtimeDatabase _database;
    private readonly string _path;

    public FakeDatabaseReference(FakeRealtimeDatabase database, string path)
    {
        _database = database;
        _path = path;
    }

    public string Key => _path.Contains('/') ? _path.Split('/').Last() : _path;
    public string Path => _path;

    public IDatabaseReference Child(string path)
    {
        return new FakeDatabaseReference(_database, $"{_path}/{path}");
    }

    public IDatabaseReference? Parent
    {
        get
        {
            if (!_path.Contains('/'))
                return null;
            var parentPath = _path.Substring(0, _path.LastIndexOf('/'));
            return new FakeDatabaseReference(_database, parentPath);
        }
    }

    // Query methods (simplified - just return self for testing)
    public IDatabaseReference OrderByChild(string path) => this;
    public IDatabaseReference OrderByKey() => this;
    public IDatabaseReference OrderByValue() => this;
    public IDatabaseReference LimitToFirst(int count) => this;
    public IDatabaseReference LimitToLast(int count) => this;
    public IDatabaseReference StartAt(object value, string? key = null) => this;
    public IDatabaseReference EndAt(object value, string? key = null) => this;
    public IDatabaseReference EqualTo(object value, string? key = null) => this;

    public Task<Result<DataSnapshot<T>>> GetAsync<T>()
    {
        if (_database.TryConsumeSimulatedError(out var error))
            return Task.FromResult(Result<DataSnapshot<T>>.Failure(error!));

        var exists = _database.HasValue(_path);
        var value = exists ? _database.GetValue<T>(_path) : default;

        return Task.FromResult(Result<DataSnapshot<T>>.Success(new DataSnapshot<T>
        {
            Key = Key,
            Exists = exists,
            Value = value
        }));
    }

    public Task<Result<Unit>> SetAsync<T>(T value)
    {
        if (_database.TryConsumeSimulatedError(out var error))
            return Task.FromResult(Result<Unit>.Failure(error!));

        _database.SetValue(_path, value!);
        return Task.FromResult(Result<Unit>.Success(Unit.Value));
    }

    public Task<Result<Unit>> UpdateAsync(object value)
    {
        if (_database.TryConsumeSimulatedError(out var error))
            return Task.FromResult(Result<Unit>.Failure(error!));

        _database.SetValue(_path, value);
        return Task.FromResult(Result<Unit>.Success(Unit.Value));
    }

    public Task<Result<PushResult>> PushAsync<T>(T value)
    {
        if (_database.TryConsumeSimulatedError(out var error))
            return Task.FromResult(Result<PushResult>.Failure(error!));

        var key = _database.GeneratePushKey();
        var childPath = $"{_path}/{key}";
        _database.SetValue(childPath, value!);

        return Task.FromResult(Result<PushResult>.Success(new PushResult
        {
            Key = key,
            Reference = new FakeDatabaseReference(_database, childPath)
        }));
    }

    public Task<Result<Unit>> RemoveAsync()
    {
        if (_database.TryConsumeSimulatedError(out var error))
            return Task.FromResult(Result<Unit>.Failure(error!));

        _database.RemoveValue(_path);
        return Task.FromResult(Result<Unit>.Success(Unit.Value));
    }

    public Task<Result<TransactionResult<T>>> TransactionAsync<T>(Func<T?, T?> updateFunction)
    {
        ArgumentNullException.ThrowIfNull(updateFunction);

        if (_database.TryConsumeSimulatedError(out var error))
            return Task.FromResult(Result<TransactionResult<T>>.Failure(error!));

        var currentValue = _database.HasValue(_path) ? _database.GetValue<T>(_path) : default;
        var newValue = updateFunction(currentValue);

        if (newValue == null)
        {
            // Transaction aborted
            return Task.FromResult(Result<TransactionResult<T>>.Success(new TransactionResult<T>
            {
                Committed = false,
                Value = currentValue
            }));
        }

        _database.SetValue(_path, newValue);
        return Task.FromResult(Result<TransactionResult<T>>.Success(new TransactionResult<T>
        {
            Committed = true,
            Value = newValue
        }));
    }

    public Action OnValue<T>(Action<DataSnapshot<T>> onNext, Action<Exception>? onError = null)
    {
        void Handler(string _)
        {
            var snapshot = GetAsync<T>().Result.Value;
            onNext(snapshot);
        }

        _database.SubscribeToValue(_path, Handler);

        // Emit initial snapshot
        Handler(_path);

        return () => _database.UnsubscribeFromValue(_path, Handler);
    }

    public Action OnChildAdded<T>(Action<DataSnapshot<T>> onNext, Action<Exception>? onError = null)
    {
        // Simplified: just use value listener
        return OnValue(onNext, onError);
    }

    public Action OnChildChanged<T>(Action<DataSnapshot<T>> onNext, Action<Exception>? onError = null)
    {
        return OnValue(onNext, onError);
    }

    public Action OnChildRemoved<T>(Action<DataSnapshot<T>> onNext, Action<Exception>? onError = null)
    {
        return OnValue(onNext, onError);
    }

    public Action OnChildMoved<T>(Action<DataSnapshot<T>> onNext, Action<Exception>? onError = null)
    {
        return OnValue(onNext, onError);
    }

    public IOnDisconnect OnDisconnect() => new FakeOnDisconnect(_database, _path);
}

internal sealed class FakeOnDisconnect : IOnDisconnect
{
    private readonly FakeRealtimeDatabase _database;
    private readonly string _path;

    public FakeOnDisconnect(FakeRealtimeDatabase database, string path)
    {
        _database = database;
        _path = path;
    }

    public Task<Result<Unit>> SetAsync<T>(T value)
    {
        if (_database.TryConsumeSimulatedError(out var error))
            return Task.FromResult(Result<Unit>.Failure(error!));

        // In fake, we just store the pending operation (or do nothing for simplicity)
        return Task.FromResult(Result<Unit>.Success(Unit.Value));
    }

    public Task<Result<Unit>> RemoveAsync()
    {
        if (_database.TryConsumeSimulatedError(out var error))
            return Task.FromResult(Result<Unit>.Failure(error!));

        return Task.FromResult(Result<Unit>.Success(Unit.Value));
    }

    public Task<Result<Unit>> UpdateAsync(object value)
    {
        if (_database.TryConsumeSimulatedError(out var error))
            return Task.FromResult(Result<Unit>.Failure(error!));

        return Task.FromResult(Result<Unit>.Success(Unit.Value));
    }

    public Task<Result<Unit>> CancelAsync()
    {
        if (_database.TryConsumeSimulatedError(out var error))
            return Task.FromResult(Result<Unit>.Failure(error!));

        return Task.FromResult(Result<Unit>.Success(Unit.Value));
    }
}
