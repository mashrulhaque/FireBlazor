namespace FireBlazor;

/// <summary>
/// Main gateway interface for all Firebase services.
/// Inject this to access Auth, Firestore, Storage, and RealtimeDb.
/// </summary>
public interface IFirebase
{
    /// <summary>Firebase Authentication service.</summary>
    IFirebaseAuth Auth { get; }

    /// <summary>Cloud Firestore database service.</summary>
    IFirestore Firestore { get; }

    /// <summary>Cloud Storage service.</summary>
    IFirebaseStorage Storage { get; }

    /// <summary>Realtime Database service.</summary>
    IRealtimeDatabase RealtimeDb { get; }

    /// <summary>App Check service for protecting backend resources.</summary>
    IAppCheck AppCheck { get; }

    /// <summary>Firebase AI Logic service for generative AI.</summary>
    IFirebaseAI AI { get; }

    /// <summary>Event raised when any Firebase operation completes.</summary>
    event Action<FirebaseOperation>? OnOperation;

    /// <summary>Event raised when any Firebase error occurs.</summary>
    event Action<FirebaseException>? OnError;

    /// <summary>
    /// Initializes Firebase services asynchronously.
    /// This must be called before using any Firebase service in WebAssembly.
    /// Sets up the Firebase app and connects to emulators if configured.
    /// </summary>
    Task InitializeAsync();
}

/// <summary>
/// Represents a completed Firebase operation for telemetry/debugging.
/// </summary>
public sealed record FirebaseOperation(
    string Service,
    string Action,
    TimeSpan Duration,
    bool Success
);
