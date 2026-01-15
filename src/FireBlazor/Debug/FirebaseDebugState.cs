using System.Diagnostics;

namespace FireBlazor;

/// <summary>
/// Tracks Firebase debug state including operations, errors, and auth state.
/// Used by FirebaseDebugPanel to display real-time debugging information.
/// Thread-safe for concurrent access from multiple threads.
/// </summary>
public sealed class FirebaseDebugState
{
    private readonly object _lock = new();
    private readonly List<FirebaseOperation> _operations = [];
    private readonly List<FirebaseException> _errors = [];
    private FirebaseUser? _currentUser;
    private bool _isAuthenticated;
    private int _activeSubscriptionCount;

    /// <summary>
    /// Maximum number of operations to keep in history.
    /// </summary>
    public int MaxOperations { get; set; } = 50;

    /// <summary>
    /// Maximum number of errors to keep in history.
    /// </summary>
    public int MaxErrors { get; set; } = 20;

    /// <summary>
    /// The currently authenticated user, if any.
    /// </summary>
    public FirebaseUser? CurrentUser
    {
        get
        {
            lock (_lock)
            {
                return _currentUser;
            }
        }
    }

    /// <summary>
    /// Whether a user is currently authenticated.
    /// </summary>
    public bool IsAuthenticated
    {
        get
        {
            lock (_lock)
            {
                return _isAuthenticated;
            }
        }
    }

    /// <summary>
    /// Number of active real-time subscriptions.
    /// </summary>
    public int ActiveSubscriptionCount
    {
        get
        {
            lock (_lock)
            {
                return _activeSubscriptionCount;
            }
        }
    }

    /// <summary>
    /// Recent Firebase operations (most recent first).
    /// </summary>
    public IReadOnlyList<FirebaseOperation> RecentOperations
    {
        get
        {
            lock (_lock)
            {
                return [.. _operations];
            }
        }
    }

    /// <summary>
    /// Recent Firebase errors (most recent first).
    /// </summary>
    public IReadOnlyList<FirebaseException> RecentErrors
    {
        get
        {
            lock (_lock)
            {
                return [.. _errors];
            }
        }
    }

    /// <summary>
    /// Event raised when state changes, allowing UI to update.
    /// </summary>
    public event Action? OnStateChanged;

    /// <summary>
    /// Adds an operation to the history.
    /// </summary>
    public void AddOperation(FirebaseOperation operation)
    {
        lock (_lock)
        {
            _operations.Insert(0, operation);
            if (_operations.Count > MaxOperations)
            {
                _operations.RemoveAt(_operations.Count - 1);
            }
        }
        RaiseStateChanged();
    }

    /// <summary>
    /// Adds an error to the history.
    /// </summary>
    public void AddError(FirebaseException error)
    {
        lock (_lock)
        {
            _errors.Insert(0, error);
            if (_errors.Count > MaxErrors)
            {
                _errors.RemoveAt(_errors.Count - 1);
            }
        }
        RaiseStateChanged();
    }

    /// <summary>
    /// Updates the authentication state.
    /// </summary>
    public void UpdateAuthState(FirebaseUser? user)
    {
        lock (_lock)
        {
            _currentUser = user;
            _isAuthenticated = user != null;
        }
        RaiseStateChanged();
    }

    /// <summary>
    /// Updates the active subscription count.
    /// </summary>
    public void UpdateSubscriptionCount(int count)
    {
        lock (_lock)
        {
            _activeSubscriptionCount = count;
        }
        RaiseStateChanged();
    }

    /// <summary>
    /// Clears all operation and error history.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _operations.Clear();
            _errors.Clear();
        }
        RaiseStateChanged();
    }

    /// <summary>
    /// Raises the OnStateChanged event with error protection.
    /// </summary>
    private void RaiseStateChanged()
    {
        try
        {
            OnStateChanged?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[FireBlazor] Error in OnStateChanged handler: {ex.Message}");
        }
    }
}
