namespace FireBlazor.Platform.Wasm;

/// <summary>
/// WebAssembly implementation of IAggregateQuery using JavaScript interop.
/// Provides count, sum, and average operations on Firestore collections.
/// </summary>
internal sealed class WasmAggregateQuery : IAggregateQuery
{
    private readonly FirebaseJsInterop _jsInterop;
    private readonly string _path;
    private readonly object? _queryParams;

    public WasmAggregateQuery(FirebaseJsInterop jsInterop, string path, object? queryParams)
    {
        ArgumentNullException.ThrowIfNull(jsInterop);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        _jsInterop = jsInterop;
        _path = path;
        _queryParams = queryParams;
    }

    /// <summary>
    /// Returns the count of documents matching the query.
    /// </summary>
    public async Task<Result<long>> CountAsync()
    {
        var result = await _jsInterop.FirestoreCountAsync(_path, _queryParams);

        if (!result.Success)
            return Result<long>.Failure(new FirebaseError(result.Error!.Code, result.Error.Message));

        return Result<long>.Success(result.Data);
    }

    /// <summary>
    /// Returns the sum of a numeric field across matching documents.
    /// </summary>
    /// <param name="field">The name of the numeric field to sum (in PascalCase - will be converted to camelCase).</param>
    public async Task<Result<double>> SumAsync(string field)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(field);

        var result = await _jsInterop.FirestoreSumAsync(_path, CamelCaseHelper.ToCamelCase(field), _queryParams);

        if (!result.Success)
            return Result<double>.Failure(new FirebaseError(result.Error!.Code, result.Error.Message));

        return Result<double>.Success(result.Data);
    }

    /// <summary>
    /// Returns the average of a numeric field across matching documents.
    /// Returns null if no documents match.
    /// </summary>
    /// <param name="field">The name of the numeric field to average (in PascalCase - will be converted to camelCase).</param>
    public async Task<Result<double?>> AverageAsync(string field)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(field);

        var result = await _jsInterop.FirestoreAverageAsync(_path, CamelCaseHelper.ToCamelCase(field), _queryParams);

        if (!result.Success)
            return Result<double?>.Failure(new FirebaseError(result.Error!.Code, result.Error.Message));

        return Result<double?>.Success(result.Data);
    }
}
