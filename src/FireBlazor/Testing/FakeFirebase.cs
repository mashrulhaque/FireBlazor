namespace FireBlazor.Testing;

/// <summary>
/// In-memory fake implementation of IFirebase gateway for testing.
/// Provides access to all fake services for configuration and assertions.
/// </summary>
/// <remarks>
/// This fake is designed for single-threaded unit tests.
/// It is NOT thread-safe for concurrent access from multiple threads.
/// </remarks>
public sealed class FakeFirebase : IFirebase
{
    private readonly FakeFirebaseAuth _auth = new();
    private readonly FakeFirestore _firestore = new();
    private readonly FakeFirebaseStorage _storage = new();
    private readonly FakeRealtimeDatabase _realtimeDb = new();
    private readonly FakeAppCheck _appCheck = new();
    private readonly FakeFirebaseAI _ai = new();

    // IFirebase interface implementation
    public IFirebaseAuth Auth => _auth;
    public IFirestore Firestore => _firestore;
    public IFirebaseStorage Storage => _storage;
    public IRealtimeDatabase RealtimeDb => _realtimeDb;
    public IAppCheck AppCheck => _appCheck;
    public IFirebaseAI AI => _ai;

    public event Action<FirebaseOperation>? OnOperation;
    public event Action<FirebaseException>? OnError;

    /// <summary>
    /// Access to the fake Auth service for configuration.
    /// </summary>
    public FakeFirebaseAuth FakeAuth => _auth;

    /// <summary>
    /// Access to the fake Firestore service for configuration.
    /// </summary>
    public FakeFirestore FakeFirestore => _firestore;

    /// <summary>
    /// Access to the fake Storage service for configuration.
    /// </summary>
    public FakeFirebaseStorage FakeStorage => _storage;

    /// <summary>
    /// Access to the fake Realtime Database service for configuration.
    /// </summary>
    public FakeRealtimeDatabase FakeRealtimeDb => _realtimeDb;

    /// <summary>
    /// Access to the fake App Check service for configuration.
    /// </summary>
    public FakeAppCheck FakeAppCheck => _appCheck;

    /// <summary>
    /// Access to the fake AI service for configuration.
    /// </summary>
    public FakeFirebaseAI FakeAI => _ai;

    /// <summary>
    /// Raises an operation event (for testing operation tracking).
    /// </summary>
    public void RaiseOperation(FirebaseOperation operation)
    {
        OnOperation?.Invoke(operation);
    }

    /// <summary>
    /// Raises an error event (for testing error handling).
    /// </summary>
    public void RaiseError(FirebaseException error)
    {
        OnError?.Invoke(error);
    }

    /// <summary>
    /// Initializes Firebase (no-op for fake implementation).
    /// </summary>
    public Task InitializeAsync()
    {
        // No initialization needed for fake implementation
        return Task.CompletedTask;
    }

    /// <summary>
    /// Resets all fake services to their initial state.
    /// </summary>
    public void Reset()
    {
        _auth.Reset();
        _firestore.Reset();
        _storage.Reset();
        _realtimeDb.Reset();
        _appCheck.Reset();
        _ai.Reset();
    }
}
