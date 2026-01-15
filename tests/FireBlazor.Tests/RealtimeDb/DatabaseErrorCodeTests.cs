namespace FireBlazor.Tests.RealtimeDb;

public class DatabaseErrorCodeTests
{
    [Theory]
    [InlineData("database/permission-denied", DatabaseErrorCode.PermissionDenied)]
    [InlineData("database/disconnected", DatabaseErrorCode.Disconnected)]
    [InlineData("database/expired-token", DatabaseErrorCode.ExpiredToken)]
    [InlineData("database/invalid-token", DatabaseErrorCode.InvalidToken)]
    [InlineData("database/max-retries", DatabaseErrorCode.MaxRetries)]
    [InlineData("database/network-error", DatabaseErrorCode.NetworkError)]
    [InlineData("database/operation-failed", DatabaseErrorCode.OperationFailed)]
    [InlineData("database/overridden-by-set", DatabaseErrorCode.OverriddenBySet)]
    [InlineData("database/unavailable", DatabaseErrorCode.Unavailable)]
    [InlineData("database/user-code-exception", DatabaseErrorCode.UserCodeException)]
    [InlineData("database/write-canceled", DatabaseErrorCode.WriteCanceled)]
    public void FromFirebaseCode_ModernFormat_MapsCorrectly(string firebaseCode, DatabaseErrorCode expected)
    {
        var result = DatabaseErrorCodeExtensions.FromFirebaseCode(firebaseCode);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("PERMISSION_DENIED", DatabaseErrorCode.PermissionDenied)]
    [InlineData("DISCONNECTED", DatabaseErrorCode.Disconnected)]
    [InlineData("EXPIRED_TOKEN", DatabaseErrorCode.ExpiredToken)]
    [InlineData("INVALID_TOKEN", DatabaseErrorCode.InvalidToken)]
    [InlineData("MAX_RETRIES", DatabaseErrorCode.MaxRetries)]
    [InlineData("NETWORK_ERROR", DatabaseErrorCode.NetworkError)]
    [InlineData("OPERATION_FAILED", DatabaseErrorCode.OperationFailed)]
    [InlineData("OVERRIDDEN_BY_SET", DatabaseErrorCode.OverriddenBySet)]
    [InlineData("UNAVAILABLE", DatabaseErrorCode.Unavailable)]
    [InlineData("USER_CODE_EXCEPTION", DatabaseErrorCode.UserCodeException)]
    [InlineData("WRITE_CANCELED", DatabaseErrorCode.WriteCanceled)]
    public void FromFirebaseCode_LegacyFormat_MapsCorrectly(string firebaseCode, DatabaseErrorCode expected)
    {
        var result = DatabaseErrorCodeExtensions.FromFirebaseCode(firebaseCode);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(DatabaseErrorCode.PermissionDenied, "database/permission-denied")]
    [InlineData(DatabaseErrorCode.Disconnected, "database/disconnected")]
    [InlineData(DatabaseErrorCode.ExpiredToken, "database/expired-token")]
    [InlineData(DatabaseErrorCode.InvalidToken, "database/invalid-token")]
    [InlineData(DatabaseErrorCode.MaxRetries, "database/max-retries")]
    [InlineData(DatabaseErrorCode.NetworkError, "database/network-error")]
    [InlineData(DatabaseErrorCode.OperationFailed, "database/operation-failed")]
    [InlineData(DatabaseErrorCode.OverriddenBySet, "database/overridden-by-set")]
    [InlineData(DatabaseErrorCode.Unavailable, "database/unavailable")]
    [InlineData(DatabaseErrorCode.UserCodeException, "database/user-code-exception")]
    [InlineData(DatabaseErrorCode.WriteCanceled, "database/write-canceled")]
    [InlineData(DatabaseErrorCode.Unknown, "database/unknown")]
    public void ToFirebaseCode_MapsCorrectly(DatabaseErrorCode code, string expected)
    {
        var result = code.ToFirebaseCode();
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("database/made-up-error")]
    [InlineData("database/nonexistent")]
    [InlineData("UNKNOWN_ERROR")]
    [InlineData("random-string")]
    [InlineData("")]
    [InlineData("storage/object-not-found")]
    public void FromFirebaseCode_UnknownCode_ReturnsUnknown(string firebaseCode)
    {
        var result = DatabaseErrorCodeExtensions.FromFirebaseCode(firebaseCode);
        Assert.Equal(DatabaseErrorCode.Unknown, result);
    }

    [Fact]
    public void FromFirebaseCode_AllEnumValues_HaveModernMapping()
    {
        // Verify all enum values except Unknown have a corresponding modern Firebase code
        var allCodes = Enum.GetValues<DatabaseErrorCode>()
            .Where(c => c != DatabaseErrorCode.Unknown);

        foreach (var code in allCodes)
        {
            var firebaseCode = code.ToFirebaseCode();
            var roundTripped = DatabaseErrorCodeExtensions.FromFirebaseCode(firebaseCode);
            Assert.Equal(code, roundTripped);
        }
    }

    [Fact]
    public void ToFirebaseCode_AllValues_ReturnModernFormat()
    {
        // All ToFirebaseCode results should use the modern "database/" prefix format
        var allCodes = Enum.GetValues<DatabaseErrorCode>();

        foreach (var code in allCodes)
        {
            var firebaseCode = code.ToFirebaseCode();
            Assert.StartsWith("database/", firebaseCode);
        }
    }
}
