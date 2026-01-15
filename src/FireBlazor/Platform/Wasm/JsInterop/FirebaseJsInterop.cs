using System.Text.Json;
using Microsoft.JSInterop;

namespace FireBlazor.Platform.Wasm;

/// <summary>
/// JavaScript interop wrapper for Firebase SDK.
/// </summary>
internal sealed class FirebaseJsInterop : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly SemaphoreSlim _moduleLock = new(1, 1);
    private IJSObjectReference? _module;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public FirebaseJsInterop(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    private async ValueTask<IJSObjectReference> GetModuleAsync()
    {
        if (_module != null) return _module;

        await _moduleLock.WaitAsync();
        try
        {
            return _module ??= await _jsRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./_content/FireBlazor/fireblazor.js");
        }
        finally
        {
            _moduleLock.Release();
        }
    }

    public async Task InitializeAsync(FirebaseOptions options)
    {
        var module = await GetModuleAsync();
        var config = new
        {
            projectId = options.ProjectId,
            apiKey = options.ApiKey,
            authDomain = options.AuthDomain ?? $"{options.ProjectId}.firebaseapp.com",
            storageBucket = options.StorageBucket ?? $"{options.ProjectId}.appspot.com",
            databaseURL = options.DatabaseUrl,
            appId = options.AppId,
            messagingSenderId = options.MessagingSenderId
        };
        await module.InvokeVoidAsync("initialize", config);
    }

    // Auth
    public async Task InitializeAuthAsync(string? emulatorHost = null)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("initializeAuth", emulatorHost);
    }

    public async Task<JsResult<JsUser>> SignInWithEmailAsync(string email, string password)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<JsUser>>("signInWithEmail", email, password);
    }

    public async Task<JsResult<JsUser>> CreateUserWithEmailAsync(string email, string password)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<JsUser>>("createUserWithEmail", email, password);
    }

    public async Task<JsResult<JsUser>> SignInWithGoogleAsync()
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<JsUser>>("signInWithGoogle");
    }

    public async Task<JsResult<JsUser>> SignInWithGitHubAsync()
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<JsUser>>("signInWithGitHub");
    }

    public async Task<JsResult<JsUser>> SignInWithMicrosoftAsync()
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<JsUser>>("signInWithMicrosoft");
    }

    public async Task<JsResult<object>> SignOutAsync()
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<object>>("signOut");
    }

    public async Task<JsResult<string>> GetIdTokenAsync(bool forceRefresh = false)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<string>>("getIdToken", forceRefresh);
    }

    public async Task<JsResult<object>> SendPasswordResetEmailAsync(string email)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<object>>("sendPasswordResetEmail", email);
    }

    public async Task<JsUser?> GetCurrentUserAsync()
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsUser?>("getCurrentUser");
    }

    // Firestore
    public async Task InitializeFirestoreAsync(FirestoreOptions? options, string? emulatorHost = null)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("initializeFirestore", new
        {
            enableOfflinePersistence = options?.OfflinePersistenceEnabled ?? false,
            emulatorHost
        });
    }

    public async Task<JsResult<JsonElement>> FirestoreGetAsync(string path)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<JsonElement>>("firestoreGet", path);
    }

    public async Task<JsResult<JsDocRef>> FirestoreAddAsync(string path, object data)
    {
        var module = await GetModuleAsync();
        // Serialize with FieldValueConverter to properly handle FieldValue sentinels
        var jsonData = JsonSerializer.SerializeToElement(data, FirestoreJsonOptions.Default);
        return await module.InvokeAsync<JsResult<JsDocRef>>("firestoreAdd", path, jsonData);
    }

    public async Task<JsResult<object>> FirestoreSetAsync(string path, object data, bool merge)
    {
        var module = await GetModuleAsync();
        // Serialize with FieldValueConverter to properly handle FieldValue sentinels
        var jsonData = JsonSerializer.SerializeToElement(data, FirestoreJsonOptions.Default);
        return await module.InvokeAsync<JsResult<object>>("firestoreSet", path, jsonData, merge);
    }

    public async Task<JsResult<object>> FirestoreUpdateAsync(string path, object data)
    {
        var module = await GetModuleAsync();
        // Serialize with FieldValueConverter to properly handle FieldValue sentinels
        var jsonData = JsonSerializer.SerializeToElement(data, FirestoreJsonOptions.Default);
        return await module.InvokeAsync<JsResult<object>>("firestoreUpdate", path, jsonData);
    }

    public async Task<JsResult<object>> FirestoreDeleteAsync(string path)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<object>>("firestoreDelete", path);
    }

    public async Task<JsResult<JsonElement>> FirestoreQueryAsync(string path, object queryParams)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<JsonElement>>("firestoreQuery", path, queryParams);
    }

    // Firestore Real-time Subscriptions
    public async Task<JsResult<JsSubscriptionResult>> FirestoreSubscribeDocumentAsync(
        string path,
        DotNetObjectReference<ISnapshotCallback> callbackRef)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<JsSubscriptionResult>>(
            "firestoreSubscribeDocument", path, callbackRef);
    }

    public async Task<JsResult<JsSubscriptionResult>> FirestoreSubscribeCollectionAsync(
        string path,
        object? queryParams,
        DotNetObjectReference<ISnapshotCallback> callbackRef)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<JsSubscriptionResult>>(
            "firestoreSubscribeCollection", path, queryParams, callbackRef);
    }

    public async Task<JsResult<object>> FirestoreUnsubscribeAsync(int subscriptionId)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<object>>("firestoreUnsubscribe", subscriptionId);
    }

    public async Task<JsResult<object?>> FirestoreBatchWriteAsync(IEnumerable<BatchOperation> operations)
    {
        var module = await GetModuleAsync();
        // Serialize with FieldValueConverter to properly handle FieldValue sentinels in operation data
        var jsonOperations = JsonSerializer.SerializeToElement(operations, FirestoreJsonOptions.Default);
        return await module.InvokeAsync<JsResult<object?>>("firestoreBatchWrite", jsonOperations);
    }

    public async Task<JsResult<List<TransactionResult>>> FirestoreRunTransactionAsync(
        IEnumerable<TransactionOperation> operations)
    {
        var module = await GetModuleAsync();
        // Serialize with FieldValueConverter to properly handle FieldValue sentinels in operation data
        var jsonOperations = JsonSerializer.SerializeToElement(operations, FirestoreJsonOptions.Default);
        return await module.InvokeAsync<JsResult<List<TransactionResult>>>(
            "firestoreRunTransaction", jsonOperations);
    }

    public async Task<JsResult<JsonElement>> FirestoreRunTransactionWithCallbackAsync<TCallback>(
        IEnumerable<string> readPaths,
        DotNetObjectReference<TCallback> callbackRef) where TCallback : class
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<JsonElement>>(
            "firestoreRunTransactionWithCallback", readPaths, callbackRef);
    }

    // Firestore Aggregate Queries
    public async Task<JsResult<long>> FirestoreCountAsync(string path, object? queryParams)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<long>>("firestoreCount", path, queryParams);
    }

    public async Task<JsResult<double>> FirestoreSumAsync(string path, string field, object? queryParams)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<double>>("firestoreSum", path, field, queryParams);
    }

    public async Task<JsResult<double?>> FirestoreAverageAsync(string path, string field, object? queryParams)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<double?>>("firestoreAverage", path, field, queryParams);
    }

    // Storage
    public async Task InitializeStorageAsync(string? emulatorHost = null)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("initializeStorage", emulatorHost);
    }

    public async Task<JsResult<JsUploadResult>> StorageUploadAsync(
        string path,
        byte[] data,
        StorageMetadata? metadata,
        Action<UploadProgress>? onProgress = null)
    {
        var module = await GetModuleAsync();

        // Convert metadata to JS-compatible object
        var jsMetadata = metadata == null ? null : new
        {
            contentType = metadata.ContentType,
            cacheControl = metadata.CacheControl,
            contentDisposition = metadata.ContentDisposition,
            contentEncoding = metadata.ContentEncoding,
            contentLanguage = metadata.ContentLanguage,
            customMetadata = metadata.CustomMetadata
        };

        if (onProgress != null)
        {
            var callbackRef = DotNetObjectReference.Create(new StorageUploadCallback(onProgress));
            try
            {
                return await module.InvokeAsync<JsResult<JsUploadResult>>(
                    "storageUpload", path, data, jsMetadata, callbackRef);
            }
            finally
            {
                callbackRef.Dispose();
            }
        }

        return await module.InvokeAsync<JsResult<JsUploadResult>>(
            "storageUpload", path, data, jsMetadata, null);
    }

    public async Task<JsResult<string>> StorageGetDownloadUrlAsync(string path)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<string>>("storageGetDownloadUrl", path);
    }

    public async Task<JsResult<object>> StorageDeleteAsync(string path)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<object>>("storageDelete", path);
    }

    public async Task<JsResult<byte[]>> StorageGetBytesAsync(string path, long maxSize)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<byte[]>>("storageGetBytes", path, maxSize);
    }

    public async Task<JsResult<StorageMetadata>> StorageGetMetadataAsync(string path)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<StorageMetadata>>("storageGetMetadata", path);
    }

    public async Task<JsResult<JsListResult>> StorageListAllAsync(string path)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<JsListResult>>("storageListAll", path);
    }

    public async Task<JsResult<JsUploadResult>> StorageUploadStringAsync(
        string path,
        string data,
        int format,
        StorageMetadata? metadata)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<JsUploadResult>>(
            "storageUploadString", path, data, format, metadata);
    }

    public async Task<JsResult<StorageMetadata>> StorageUpdateMetadataAsync(
        string path,
        StorageMetadata metadata)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<StorageMetadata>>(
            "storageUpdateMetadata", path, metadata);
    }

    public async Task<JsResult<JsPagedListResult>> StorageListAsync(
        string path,
        int maxResults,
        string? pageToken)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<JsPagedListResult>>(
            "storageList", path, maxResults, pageToken);
    }

    // Realtime Database
    public async Task InitializeDatabaseAsync(string? url, string? emulatorHost = null)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("initializeDatabase", new
        {
            url,
            emulatorHost
        });
    }

    public async Task<JsResult<JsDataSnapshot>> DatabaseGetAsync(string path)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<JsDataSnapshot>>("databaseGet", path);
    }

    public async Task<JsResult<object>> DatabaseSetAsync(string path, object value)
    {
        var module = await GetModuleAsync();
        // Serialize with ServerValueConverter to properly handle ServerValue sentinels
        var jsonData = JsonSerializer.SerializeToElement(value, DatabaseJsonOptions.Default);
        return await module.InvokeAsync<JsResult<object>>("databaseSet", path, jsonData);
    }

    public async Task<JsResult<object>> DatabaseUpdateAsync(string path, object value)
    {
        var module = await GetModuleAsync();
        // Serialize with ServerValueConverter to properly handle ServerValue sentinels
        var jsonData = JsonSerializer.SerializeToElement(value, DatabaseJsonOptions.Default);
        return await module.InvokeAsync<JsResult<object>>("databaseUpdate", path, jsonData);
    }

    public async Task<JsResult<JsPushResult>> DatabasePushAsync(string path, object value)
    {
        var module = await GetModuleAsync();
        // Serialize with ServerValueConverter to properly handle ServerValue sentinels
        var jsonData = JsonSerializer.SerializeToElement(value, DatabaseJsonOptions.Default);
        return await module.InvokeAsync<JsResult<JsPushResult>>("databasePush", path, jsonData);
    }

    public async Task<JsResult<object>> DatabaseRemoveAsync(string path)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<object>>("databaseRemove", path);
    }

    public async Task<JsResult<JsDataSnapshot>> DatabaseQueryAsync(string path, object queryParams)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<JsDataSnapshot>>("databaseQuery", path, queryParams);
    }

    public async Task<JsResult<JsSubscriptionResult>> DatabaseSubscribeValueAsync(
        string path,
        object? queryParams,
        DotNetObjectReference<IDatabaseSnapshotCallback> callbackRef)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<JsSubscriptionResult>>(
            "databaseSubscribeValue", path, queryParams, callbackRef);
    }

    public async Task<JsResult<JsSubscriptionResult>> DatabaseSubscribeChildAsync(
        string path,
        string eventType,
        object? queryParams,
        DotNetObjectReference<IDatabaseSnapshotCallback> callbackRef)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<JsSubscriptionResult>>(
            "databaseSubscribeChild", path, eventType, queryParams, callbackRef);
    }

    public async Task<JsResult<object>> DatabaseUnsubscribeAsync(int subscriptionId)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<object>>("databaseUnsubscribe", subscriptionId);
    }

    public async Task<JsResult<JsTransactionResult>> DatabaseRunTransactionAsync<TCallback>(
        string path,
        DotNetObjectReference<TCallback> callbackRef) where TCallback : class
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<JsTransactionResult>>(
            "databaseRunTransaction", path, callbackRef);
    }

    public async Task<JsResult<object>> DatabaseOnDisconnectSetAsync(string path, object value)
    {
        var module = await GetModuleAsync();
        var jsonData = JsonSerializer.SerializeToElement(value, DatabaseJsonOptions.Default);
        return await module.InvokeAsync<JsResult<object>>("databaseOnDisconnectSet", path, jsonData);
    }

    public async Task<JsResult<object>> DatabaseOnDisconnectRemoveAsync(string path)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<object>>("databaseOnDisconnectRemove", path);
    }

    public async Task<JsResult<object>> DatabaseOnDisconnectUpdateAsync(string path, object value)
    {
        var module = await GetModuleAsync();
        var jsonData = JsonSerializer.SerializeToElement(value, DatabaseJsonOptions.Default);
        return await module.InvokeAsync<JsResult<object>>("databaseOnDisconnectUpdate", path, jsonData);
    }

    public async Task<JsResult<object>> DatabaseOnDisconnectCancelAsync(string path)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<object>>("databaseOnDisconnectCancel", path);
    }

    public async Task<JsResult<JsSubscriptionResult>> DatabaseSubscribeConnectionStateAsync<TCallback>(
        DotNetObjectReference<TCallback> callbackRef) where TCallback : class
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<JsSubscriptionResult>>(
            "databaseSubscribeConnectionState", callbackRef);
    }

    public async Task<JsResult<object>> DatabaseGoOfflineAsync()
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<object>>("databaseGoOffline");
    }

    public async Task<JsResult<object>> DatabaseGoOnlineAsync()
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<object>>("databaseGoOnline");
    }

    // App Check
    public async Task InitializeAppCheckAsync(AppCheckOptions? options)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("initializeAppCheck", new
        {
            reCaptchaSiteKey = options?.ReCaptchaSiteKey,
            reCaptchaEnterpriseSiteKey = options?.ReCaptchaEnterpriseSiteKey,
            debugMode = options?.DebugMode ?? false,
            debugToken = options?.DebugToken,
            autoDetectDebugMode = options?.AutoDetectDebugMode ?? false,
            isTokenAutoRefreshEnabled = options?.IsTokenAutoRefreshEnabled ?? true
        });
    }

    public async Task<JsResult<object>> AppCheckActivateAsync()
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<object>>("appCheckActivate");
    }

    public async Task<JsResult<JsAppCheckToken>> AppCheckGetTokenAsync(bool forceRefresh)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<JsAppCheckToken>>("appCheckGetToken", forceRefresh);
    }

    public async Task<JsResult<JsSubscriptionResult>> AppCheckSubscribeTokenChangedAsync<TCallback>(
        DotNetObjectReference<TCallback> callbackRef) where TCallback : class
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<JsSubscriptionResult>>("appCheckOnTokenChanged", callbackRef);
    }

    public async Task<JsResult<object>> AppCheckUnsubscribeTokenChangedAsync(int subscriptionId)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<object>>("appCheckUnsubscribeTokenChanged", subscriptionId);
    }

    [Obsolete("Token auto-refresh is configured during initialization. This method has no effect at runtime.")]
    public async Task<JsResult<object>> AppCheckSetTokenAutoRefreshEnabledAsync(bool enabled)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<object>>("appCheckSetTokenAutoRefreshEnabled", enabled);
    }

    // AI Logic
    public async Task InitializeAIAsync(string backend)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("initializeAI", backend);
    }

    public async Task<JsResult<JsModelRef>> AIGetGenerativeModelAsync(string modelName, object? config)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<JsModelRef>>("aiGetGenerativeModel", modelName, config);
    }

    public async Task<JsResult<JsGenerateContentResult>> AIGenerateContentAsync(string modelName, string prompt)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<JsGenerateContentResult>>("aiGenerateContent", modelName, prompt);
    }

    public async Task<JsResult<JsGenerateContentResult>> AIGenerateContentWithPartsAsync(string modelName, object[] parts)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<JsGenerateContentResult>>("generateContentWithParts", modelName, parts);
    }

    public async Task AIGenerateContentStreamAsync<TCallback>(
        string modelName,
        string prompt,
        DotNetObjectReference<TCallback> callbackRef,
        string callbackMethod) where TCallback : class
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("aiGenerateContentStream", modelName, prompt, callbackRef, callbackMethod);
    }

    public async Task AIGenerateContentStreamWithPartsAsync<TCallback>(
        string modelName,
        object[] parts,
        DotNetObjectReference<TCallback> callbackRef,
        string callbackMethod) where TCallback : class
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("generateContentStreamWithParts", modelName, parts, callbackRef, callbackMethod);
    }

    public async Task<JsResult<JsChatSessionRef>> AIStartChatAsync(string modelName, string? historyJson)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<JsChatSessionRef>>("aiStartChat", modelName, historyJson);
    }

    public async Task<JsResult<JsGenerateContentResult>> AISendChatMessageAsync(int sessionId, string message)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<JsGenerateContentResult>>("aiSendChatMessage", sessionId, message);
    }

    public async Task<JsResult<JsGenerateContentResult>> AISendChatMessageWithPartsAsync(int sessionId, object[] parts)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<JsGenerateContentResult>>("aiSendChatMessageWithParts", sessionId, parts);
    }

    public async Task AISendChatMessageStreamAsync<TCallback>(
        int sessionId,
        string message,
        DotNetObjectReference<TCallback> callbackRef,
        string callbackMethod) where TCallback : class
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("aiSendChatMessageStream", sessionId, message, callbackRef, callbackMethod);
    }

    public async Task AISendChatMessageStreamWithPartsAsync<TCallback>(
        int sessionId,
        object[] parts,
        DotNetObjectReference<TCallback> callbackRef,
        string callbackMethod) where TCallback : class
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("aiSendChatMessageStreamWithParts", sessionId, parts, callbackRef, callbackMethod);
    }

    public async Task<JsResult<object>> AIDisposeChatSessionAsync(int sessionId)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<object>>("aiDisposeChatSession", sessionId);
    }

    public async Task<JsResult<JsTokenCount>> AICountTokensAsync(string modelName, string text)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<JsTokenCount>>("countTokens", modelName, text);
    }

    public async Task<JsResult<JsTokenCount>> AICountTokensWithPartsAsync(string modelName, object[] parts)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<JsTokenCount>>("countTokens", modelName, parts);
    }

    // AI Logic - Image Generation (Imagen)
    public async Task<JsResult<JsModelRef>> AIGetImageModelAsync(string modelName)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<JsModelRef>>("aiGetImageModel", modelName);
    }

    public async Task<JsResult<JsImageGenerationResponse>> AIGenerateImagesAsync(
        string modelName,
        string prompt,
        object? config)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsResult<JsImageGenerationResponse>>("aiGenerateImages", modelName, prompt, config);
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            await _module.DisposeAsync();
        }
        _moduleLock.Dispose();
    }
}

// JS Interop DTOs
internal sealed class JsResult<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public JsError? Error { get; set; }
}

internal sealed class JsError
{
    public string Code { get; set; } = "";
    public string Message { get; set; } = "";
}

internal sealed class JsUser
{
    public string Uid { get; set; } = "";
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public string? PhotoUrl { get; set; }
    public bool IsEmailVerified { get; set; }
    public bool IsAnonymous { get; set; }
    public List<string> Providers { get; set; } = [];
    public string? CreatedAt { get; set; }
    public string? LastSignInAt { get; set; }
}

internal sealed class JsDocRef
{
    public string Id { get; set; } = "";
    public string Path { get; set; } = "";
}

internal sealed class JsUploadResult
{
    public string DownloadUrl { get; set; } = "";
    public string FullPath { get; set; } = "";
    public long BytesTransferred { get; set; }
}

internal sealed class JsDataSnapshot
{
    public string? Key { get; set; }
    public bool Exists { get; set; }
    public JsonElement? Value { get; set; }
}

internal sealed class JsPushResult
{
    public string Key { get; set; } = "";
}

internal sealed class JsTransactionResult
{
    public bool Committed { get; set; }
    public JsonElement? Value { get; set; }
}

internal sealed class JsSubscriptionResult
{
    public int SubscriptionId { get; set; }
}

/// <summary>
/// Interface for Firestore snapshot callback handlers used in real-time subscriptions.
/// </summary>
internal interface ISnapshotCallback
{
    [JSInvokable]
    void OnDocumentSnapshot(JsonElement data);

    [JSInvokable]
    void OnCollectionSnapshot(JsonElement[] data);

    [JSInvokable]
    void OnSnapshotError(JsError error);
}

/// <summary>
/// Interface for Realtime Database snapshot callback handlers used in real-time subscriptions.
/// </summary>
internal interface IDatabaseSnapshotCallback
{
    [JSInvokable]
    void OnDataSnapshot(JsonElement data);

    [JSInvokable]
    void OnSnapshotError(JsError error);
}

/// <summary>
/// Interface for Realtime Database transaction callback handlers.
/// </summary>
internal interface ITransactionCallback
{
    [JSInvokable]
    object? OnTransactionUpdate(JsonElement? currentData);
}

/// <summary>
/// Interface for connection state change callback handlers.
/// </summary>
internal interface IConnectionStateCallback
{
    [JSInvokable]
    void OnConnectionStateChanged(bool isConnected);
}

/// <summary>
/// Interface for App Check token change callback handlers.
/// </summary>
internal interface IAppCheckTokenCallback
{
    [JSInvokable]
    void OnTokenChanged(JsAppCheckToken token);
}

internal sealed class JsListResult
{
    public List<string> Items { get; set; } = [];
    public List<string> Prefixes { get; set; } = [];
}

internal sealed class JsPagedListResult
{
    public List<string> Items { get; set; } = [];
    public List<string> Prefixes { get; set; } = [];
    public string? NextPageToken { get; set; }
}

/// <summary>
/// Callback handler for storage upload progress.
/// </summary>
internal sealed class StorageUploadCallback
{
    private readonly Action<UploadProgress> _onProgress;

    public StorageUploadCallback(Action<UploadProgress> onProgress)
    {
        _onProgress = onProgress;
    }

    [JSInvokable]
    public void OnProgress(JsUploadProgress progress)
    {
        _onProgress(new UploadProgress
        {
            BytesTransferred = progress.BytesTransferred,
            TotalBytes = progress.TotalBytes
        });
    }
}

internal sealed class JsUploadProgress
{
    public long BytesTransferred { get; set; }
    public long TotalBytes { get; set; }
}

internal sealed class JsAppCheckToken
{
    public string Token { get; set; } = "";
    public long ExpireTimeMillis { get; set; }
}

/// <summary>
/// Represents a single operation in a Firestore batch write.
/// </summary>
internal sealed class BatchOperation
{
    /// <summary>
    /// The type of operation: "set", "update", or "delete".
    /// </summary>
    public string Type { get; set; } = "";

    /// <summary>
    /// The document path (e.g., "users/123").
    /// </summary>
    public string Path { get; set; } = "";

    /// <summary>
    /// The data to write (for set and update operations).
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// Whether to merge data with existing document (for set operations).
    /// </summary>
    public bool Merge { get; set; }
}

/// <summary>
/// Represents a single operation in a Firestore transaction.
/// </summary>
internal sealed class TransactionOperation
{
    /// <summary>
    /// The type of operation: "get", "set", "update", or "delete".
    /// </summary>
    public string Type { get; set; } = "";

    /// <summary>
    /// The document path (e.g., "users/123").
    /// </summary>
    public string Path { get; set; } = "";

    /// <summary>
    /// The data to write (for set and update operations).
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// Whether to merge data with existing document (for set operations).
    /// </summary>
    public bool Merge { get; set; }
}

/// <summary>
/// Represents the result of a single operation in a Firestore transaction.
/// </summary>
internal sealed class TransactionResult
{
    /// <summary>
    /// The type of operation that was performed.
    /// </summary>
    public string Type { get; set; } = "";

    /// <summary>
    /// The document path.
    /// </summary>
    public string Path { get; set; } = "";

    /// <summary>
    /// Whether the document exists (for get operations).
    /// </summary>
    public bool Exists { get; set; }

    /// <summary>
    /// The document data (for get operations).
    /// </summary>
    public JsonElement? Data { get; set; }

    /// <summary>
    /// The document ID (for get operations).
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Whether the operation succeeded (for write operations).
    /// </summary>
    public bool Success { get; set; }
}

// AI Logic DTOs
internal sealed class JsModelRef
{
    public string ModelName { get; set; } = "";
}

internal sealed class JsGenerateContentResult
{
    public string Text { get; set; } = "";
    public JsTokenUsage? Usage { get; set; }
    public int FinishReason { get; set; }
    public IReadOnlyList<JsSafetyRating>? SafetyRatings { get; set; }
    public IReadOnlyList<JsFunctionCall>? FunctionCalls { get; set; }
    public JsGroundingMetadata? GroundingMetadata { get; set; }
}

internal sealed class JsFunctionCall
{
    public string Name { get; set; } = "";
    public System.Text.Json.JsonElement Arguments { get; set; }
}

internal sealed class JsSafetyRating
{
    public int Category { get; set; }
    public int Probability { get; set; }
    public bool Blocked { get; set; }
}

internal sealed class JsTokenUsage
{
    public int PromptTokens { get; set; }
    public int CandidateTokens { get; set; }
    public int TotalTokens { get; set; }
}

internal sealed class JsStreamChunk
{
    public string Text { get; set; } = "";
    public bool IsFinal { get; set; }
}

internal sealed class JsChatSessionRef
{
    public int SessionId { get; set; }
}

internal sealed class JsTokenCount
{
    public int TotalTokens { get; set; }
    public int? TextTokens { get; set; }
    public int? ImageTokens { get; set; }
    public int? AudioTokens { get; set; }
    public int? VideoTokens { get; set; }
}

// AI Logic - Image Generation DTOs
internal sealed class JsImageGenerationResponse
{
    public JsGeneratedImage[] Images { get; set; } = [];
}

internal sealed class JsGeneratedImage
{
    public string Base64Data { get; set; } = "";
    public string MimeType { get; set; } = "";
}

// Grounding DTOs
internal sealed class JsGroundingMetadata
{
    public IReadOnlyList<string>? SearchQueries { get; set; }
    public IReadOnlyList<JsGroundingChunk>? GroundingChunks { get; set; }
    public IReadOnlyList<JsGroundingSupport>? GroundingSupports { get; set; }
    public JsSearchEntryPoint? SearchEntryPoint { get; set; }
}

internal sealed class JsGroundingChunk
{
    public JsWebSource? Web { get; set; }
}

internal sealed class JsWebSource
{
    public string? Uri { get; set; }
    public string? Title { get; set; }
}

internal sealed class JsGroundingSupport
{
    public JsGroundingSegment? Segment { get; set; }
    public IReadOnlyList<int>? GroundingChunkIndices { get; set; }
    public IReadOnlyList<float>? ConfidenceScores { get; set; }
}

internal sealed class JsGroundingSegment
{
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
    public string? Text { get; set; }
}

internal sealed class JsSearchEntryPoint
{
    public string? RenderedContent { get; set; }
    public string? SdkBlob { get; set; }
}
