using Microsoft.AspNetCore.Components.Forms;

namespace FireBlazor.Platform.Wasm;

/// <summary>
/// WebAssembly implementation of IStorageReference using JavaScript interop.
/// </summary>
internal sealed class WasmStorageReference : IStorageReference
{
    private readonly FirebaseJsInterop _jsInterop;
    private readonly string _path;
    private readonly long _maxBrowserFileSize;

    public WasmStorageReference(FirebaseJsInterop jsInterop, string path, long maxBrowserFileSize = StorageOptions.DefaultMaxBrowserFileSize)
    {
        ArgumentNullException.ThrowIfNull(jsInterop);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _jsInterop = jsInterop;
        _path = path;
        _maxBrowserFileSize = maxBrowserFileSize;
    }

    public string Name => _path.Split('/').Last();

    public string FullPath => _path;

    public IStorageReference Child(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var combinedPath = $"{_path.TrimEnd('/')}/{path.TrimStart('/')}";
        return new WasmStorageReference(_jsInterop, combinedPath, _maxBrowserFileSize);
    }

    public IStorageReference? Parent
    {
        get
        {
            var lastSlash = _path.LastIndexOf('/');
            if (lastSlash <= 0)
                return null;

            var parentPath = _path[..lastSlash];
            return new WasmStorageReference(_jsInterop, parentPath, _maxBrowserFileSize);
        }
    }

    public async Task<Result<UploadResult>> PutAsync(
        Stream data,
        StorageMetadata? metadata = null,
        Action<UploadProgress>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var ms = new MemoryStream();
            await data.CopyToAsync(ms, cancellationToken);
            var bytes = ms.ToArray();

            cancellationToken.ThrowIfCancellationRequested();

            return await UploadBytesAsync(bytes, metadata, onProgress, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return CreateFailureResult<UploadResult>("storage/canceled", "Upload was canceled");
        }
        catch (Exception ex)
        {
            return CreateFailureResult<UploadResult>("storage/unknown", ex.Message);
        }
    }

    public async Task<Result<UploadResult>> PutAsync(
        IBrowserFile file,
        StorageMetadata? metadata = null,
        Action<UploadProgress>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var effectiveMetadata = metadata ?? new StorageMetadata();
            if (string.IsNullOrEmpty(effectiveMetadata.ContentType))
            {
                effectiveMetadata.ContentType = file.ContentType;
            }

            await using var stream = file.OpenReadStream(_maxBrowserFileSize);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, cancellationToken);
            var bytes = ms.ToArray();

            cancellationToken.ThrowIfCancellationRequested();

            return await UploadBytesAsync(bytes, effectiveMetadata, onProgress, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return CreateFailureResult<UploadResult>("storage/canceled", "Upload was canceled");
        }
        catch (Exception ex)
        {
            return CreateFailureResult<UploadResult>("storage/unknown", ex.Message);
        }
    }

    private async Task<Result<UploadResult>> UploadBytesAsync(
        byte[] bytes,
        StorageMetadata? metadata,
        Action<UploadProgress>? onProgress,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await _jsInterop.StorageUploadAsync(_path, bytes, metadata, onProgress);

            if (result.Success && result.Data != null)
            {
                return Result<UploadResult>.Success(new UploadResult
                {
                    DownloadUrl = result.Data.DownloadUrl,
                    FullPath = result.Data.FullPath,
                    BytesTransferred = result.Data.BytesTransferred
                });
            }

            return CreateFailureFromJsError<UploadResult>(result.Error);
        }
        catch (OperationCanceledException)
        {
            return CreateFailureResult<UploadResult>("storage/canceled", "Upload was canceled");
        }
        catch (Exception ex)
        {
            return CreateFailureResult<UploadResult>("storage/unknown", ex.Message);
        }
    }

    public async Task<Result<byte[]>> GetBytesAsync(long maxSize = 10 * 1024 * 1024)
    {
        try
        {
            var result = await _jsInterop.StorageGetBytesAsync(_path, maxSize);

            if (result.Success && result.Data != null)
            {
                return Result<byte[]>.Success(result.Data);
            }

            return CreateFailureFromJsError<byte[]>(result.Error);
        }
        catch (Exception ex)
        {
            return CreateFailureResult<byte[]>("storage/unknown", ex.Message);
        }
    }

    public async Task<Result<string>> GetDownloadUrlAsync()
    {
        try
        {
            var result = await _jsInterop.StorageGetDownloadUrlAsync(_path);

            if (result.Success && result.Data != null)
            {
                return Result<string>.Success(result.Data);
            }

            return CreateFailureFromJsError<string>(result.Error);
        }
        catch (Exception ex)
        {
            return CreateFailureResult<string>("storage/unknown", ex.Message);
        }
    }

    public async Task<Result<StorageMetadata>> GetMetadataAsync()
    {
        try
        {
            var result = await _jsInterop.StorageGetMetadataAsync(_path);

            if (result.Success && result.Data != null)
            {
                return Result<StorageMetadata>.Success(result.Data);
            }

            return CreateFailureFromJsError<StorageMetadata>(result.Error);
        }
        catch (Exception ex)
        {
            return CreateFailureResult<StorageMetadata>("storage/unknown", ex.Message);
        }
    }

    public async Task<Result<Unit>> DeleteAsync()
    {
        try
        {
            var result = await _jsInterop.StorageDeleteAsync(_path);

            if (result.Success)
            {
                return Result<Unit>.Success(Unit.Value);
            }

            return CreateFailureFromJsError<Unit>(result.Error);
        }
        catch (Exception ex)
        {
            return CreateFailureResult<Unit>("storage/unknown", ex.Message);
        }
    }

    public async Task<Result<ListResult>> ListAllAsync()
    {
        try
        {
            var result = await _jsInterop.StorageListAllAsync(_path);

            if (result.Success && result.Data != null)
            {
                var items = result.Data.Items
                    .Select(path => (IStorageReference)new WasmStorageReference(_jsInterop, path))
                    .ToList();

                var prefixes = result.Data.Prefixes
                    .Select(path => (IStorageReference)new WasmStorageReference(_jsInterop, path))
                    .ToList();

                return Result<ListResult>.Success(new ListResult
                {
                    Items = items,
                    Prefixes = prefixes
                });
            }

            return CreateFailureFromJsError<ListResult>(result.Error);
        }
        catch (Exception ex)
        {
            return CreateFailureResult<ListResult>("storage/unknown", ex.Message);
        }
    }

    public async Task<Result<UploadResult>> PutStringAsync(
        string data,
        StringFormat format = StringFormat.Raw,
        StorageMetadata? metadata = null,
        Action<UploadProgress>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await _jsInterop.StorageUploadStringAsync(_path, data, (int)format, metadata);

            if (result.Success && result.Data != null)
            {
                onProgress?.Invoke(new UploadProgress
                {
                    BytesTransferred = result.Data.BytesTransferred,
                    TotalBytes = result.Data.BytesTransferred
                });

                return Result<UploadResult>.Success(new UploadResult
                {
                    DownloadUrl = result.Data.DownloadUrl,
                    FullPath = result.Data.FullPath,
                    BytesTransferred = result.Data.BytesTransferred
                });
            }

            return CreateFailureFromJsError<UploadResult>(result.Error);
        }
        catch (OperationCanceledException)
        {
            return CreateFailureResult<UploadResult>("storage/canceled", "Upload was canceled");
        }
        catch (Exception ex)
        {
            return CreateFailureResult<UploadResult>("storage/unknown", ex.Message);
        }
    }

    public async Task<Result<StorageMetadata>> UpdateMetadataAsync(StorageMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        try
        {
            var result = await _jsInterop.StorageUpdateMetadataAsync(_path, metadata);

            if (result.Success && result.Data != null)
            {
                return Result<StorageMetadata>.Success(result.Data);
            }

            return CreateFailureFromJsError<StorageMetadata>(result.Error);
        }
        catch (Exception ex)
        {
            return CreateFailureResult<StorageMetadata>("storage/unknown", ex.Message);
        }
    }

    public async Task<Result<PagedListResult>> ListAsync(ListOptions? options = null)
    {
        try
        {
            var maxResults = options?.MaxResults ?? 1000;
            var pageToken = options?.PageToken;

            var result = await _jsInterop.StorageListAsync(_path, maxResults, pageToken);

            if (result.Success && result.Data != null)
            {
                var items = result.Data.Items
                    .Select(path => (IStorageReference)new WasmStorageReference(_jsInterop, path, _maxBrowserFileSize))
                    .ToList();

                var prefixes = result.Data.Prefixes
                    .Select(path => (IStorageReference)new WasmStorageReference(_jsInterop, path, _maxBrowserFileSize))
                    .ToList();

                return Result<PagedListResult>.Success(new PagedListResult
                {
                    Items = items,
                    Prefixes = prefixes,
                    NextPageToken = result.Data.NextPageToken
                });
            }

            return CreateFailureFromJsError<PagedListResult>(result.Error);
        }
        catch (Exception ex)
        {
            return CreateFailureResult<PagedListResult>("storage/unknown", ex.Message);
        }
    }

    public async Task<Result<Stream>> GetStreamAsync(long maxSize = 10 * 1024 * 1024)
    {
        try
        {
            var bytesResult = await GetBytesAsync(maxSize);

            if (bytesResult.IsSuccess)
            {
                return Result<Stream>.Success(new MemoryStream(bytesResult.Value));
            }

            return Result<Stream>.Failure(bytesResult.Error!);
        }
        catch (Exception ex)
        {
            return CreateFailureResult<Stream>("storage/unknown", ex.Message);
        }
    }

    /// <summary>
    /// Creates a failure result from a JS error, normalizing the error code through StorageErrorCode.
    /// </summary>
    private static Result<T> CreateFailureFromJsError<T>(JsError? error)
    {
        var code = error?.Code ?? "storage/unknown";
        var message = error?.Message ?? "Unknown error";

        // Normalize error code through StorageErrorCode
        var storageCode = StorageErrorCodeExtensions.FromFirebaseCode(code);
        return Result<T>.Failure(new FirebaseError(storageCode.ToFirebaseCode(), message));
    }

    /// <summary>
    /// Creates a failure result with the given code and message, normalizing through StorageErrorCode.
    /// </summary>
    private static Result<T> CreateFailureResult<T>(string code, string message)
    {
        var storageCode = StorageErrorCodeExtensions.FromFirebaseCode(code);
        return Result<T>.Failure(new FirebaseError(storageCode.ToFirebaseCode(), message));
    }
}
