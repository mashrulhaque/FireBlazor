namespace FireBlazor.Tests.Storage;

public class StorageErrorCodeTests
{
    [Theory]
    [InlineData("storage/object-not-found", StorageErrorCode.ObjectNotFound)]
    [InlineData("storage/bucket-not-found", StorageErrorCode.BucketNotFound)]
    [InlineData("storage/project-not-found", StorageErrorCode.ProjectNotFound)]
    [InlineData("storage/quota-exceeded", StorageErrorCode.QuotaExceeded)]
    [InlineData("storage/unauthenticated", StorageErrorCode.Unauthenticated)]
    [InlineData("storage/unauthorized", StorageErrorCode.Unauthorized)]
    [InlineData("storage/retry-limit-exceeded", StorageErrorCode.RetryLimitExceeded)]
    [InlineData("storage/invalid-checksum", StorageErrorCode.InvalidChecksum)]
    [InlineData("storage/canceled", StorageErrorCode.Canceled)]
    [InlineData("storage/invalid-url", StorageErrorCode.InvalidUrl)]
    [InlineData("storage/invalid-argument", StorageErrorCode.InvalidArgument)]
    [InlineData("storage/no-default-bucket", StorageErrorCode.NoDefaultBucket)]
    [InlineData("storage/cannot-slice-blob", StorageErrorCode.CannotSliceBlob)]
    [InlineData("storage/server-file-wrong-size", StorageErrorCode.ServerFileWrongSize)]
    [InlineData("storage/unknown-error", StorageErrorCode.Unknown)]
    public void FromFirebaseCode_MapsCorrectly(string firebaseCode, StorageErrorCode expected)
    {
        var result = StorageErrorCodeExtensions.FromFirebaseCode(firebaseCode);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(StorageErrorCode.ObjectNotFound, "storage/object-not-found")]
    [InlineData(StorageErrorCode.BucketNotFound, "storage/bucket-not-found")]
    [InlineData(StorageErrorCode.ProjectNotFound, "storage/project-not-found")]
    [InlineData(StorageErrorCode.QuotaExceeded, "storage/quota-exceeded")]
    [InlineData(StorageErrorCode.Unauthenticated, "storage/unauthenticated")]
    [InlineData(StorageErrorCode.Unauthorized, "storage/unauthorized")]
    [InlineData(StorageErrorCode.RetryLimitExceeded, "storage/retry-limit-exceeded")]
    [InlineData(StorageErrorCode.InvalidChecksum, "storage/invalid-checksum")]
    [InlineData(StorageErrorCode.Canceled, "storage/canceled")]
    [InlineData(StorageErrorCode.InvalidUrl, "storage/invalid-url")]
    [InlineData(StorageErrorCode.InvalidArgument, "storage/invalid-argument")]
    [InlineData(StorageErrorCode.NoDefaultBucket, "storage/no-default-bucket")]
    [InlineData(StorageErrorCode.CannotSliceBlob, "storage/cannot-slice-blob")]
    [InlineData(StorageErrorCode.ServerFileWrongSize, "storage/server-file-wrong-size")]
    [InlineData(StorageErrorCode.Unknown, "storage/unknown")]
    public void ToFirebaseCode_MapsCorrectly(StorageErrorCode code, string expected)
    {
        var result = code.ToFirebaseCode();
        Assert.Equal(expected, result);
    }

    [Fact]
    public void StorageException_ContainsCode()
    {
        var ex = new StorageException(StorageErrorCode.ObjectNotFound, "File not found");

        Assert.Equal(StorageErrorCode.ObjectNotFound, ex.StorageCode);
        Assert.Equal("storage/object-not-found", ex.Code);
        Assert.Equal("File not found", ex.Message);
    }

    [Fact]
    public void StorageException_WithInnerException_PreservesException()
    {
        var inner = new InvalidOperationException("Inner error");
        var ex = new StorageException(StorageErrorCode.Unknown, "Outer error", inner);

        Assert.Equal(StorageErrorCode.Unknown, ex.StorageCode);
        Assert.Equal("storage/unknown", ex.Code);
        Assert.Equal("Outer error", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }
}
