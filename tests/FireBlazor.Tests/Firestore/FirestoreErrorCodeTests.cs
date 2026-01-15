namespace FireBlazor.Tests.Firestore;

public class FirestoreErrorCodeTests
{
    [Theory]
    [InlineData("permission-denied", FirestoreErrorCode.PermissionDenied)]
    [InlineData("not-found", FirestoreErrorCode.NotFound)]
    [InlineData("already-exists", FirestoreErrorCode.AlreadyExists)]
    [InlineData("failed-precondition", FirestoreErrorCode.FailedPrecondition)]
    [InlineData("aborted", FirestoreErrorCode.Aborted)]
    [InlineData("unavailable", FirestoreErrorCode.Unavailable)]
    [InlineData("cancelled", FirestoreErrorCode.Cancelled)]
    [InlineData("invalid-argument", FirestoreErrorCode.InvalidArgument)]
    [InlineData("unknown-error", FirestoreErrorCode.Unknown)]
    public void FromFirebaseCode_MapsCorrectly(string firebaseCode, FirestoreErrorCode expected)
    {
        var result = FirestoreErrorCodeExtensions.FromFirebaseCode(firebaseCode);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToFirebaseCode_MapsCorrectly()
    {
        Assert.Equal("permission-denied", FirestoreErrorCode.PermissionDenied.ToFirebaseCode());
        Assert.Equal("not-found", FirestoreErrorCode.NotFound.ToFirebaseCode());
        Assert.Equal("already-exists", FirestoreErrorCode.AlreadyExists.ToFirebaseCode());
    }

    [Fact]
    public void FirestoreException_ContainsCode()
    {
        var ex = new FirestoreException(FirestoreErrorCode.PermissionDenied, "Access denied to document");

        Assert.Equal(FirestoreErrorCode.PermissionDenied, ex.FirestoreCode);
        Assert.Equal("permission-denied", ex.Code);
        Assert.Equal("Access denied to document", ex.Message);
    }

    [Fact]
    public void FirestoreException_WithInnerException()
    {
        var inner = new InvalidOperationException("Inner error");
        var ex = new FirestoreException(FirestoreErrorCode.Aborted, "Transaction aborted", inner);

        Assert.Equal(FirestoreErrorCode.Aborted, ex.FirestoreCode);
        Assert.Same(inner, ex.InnerException);
    }
}
