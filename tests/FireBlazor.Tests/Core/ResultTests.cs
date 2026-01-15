namespace FireBlazor.Tests.Core;

public class ResultTests
{
    [Fact]
    public void Success_CreatesSuccessfulResult()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(42, result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_CreatesFailedResult()
    {
        var error = new FirebaseError("auth/invalid-email", "Invalid email");
        var result = Result<int>.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void Value_ThrowsOnFailure()
    {
        var error = new FirebaseError("test/error", "Test error");
        var result = Result<int>.Failure(error);

        Assert.Throws<InvalidOperationException>(() => _ = result.Value);
    }

    [Fact]
    public void ImplicitConversion_FromValue()
    {
        Result<string> result = "hello";

        Assert.True(result.IsSuccess);
        Assert.Equal("hello", result.Value);
    }

    [Fact]
    public void Match_ExecutesCorrectBranch()
    {
        var success = Result<int>.Success(10);
        var failure = Result<int>.Failure(new FirebaseError("err", "msg"));

        var successResult = success.Match(
            onSuccess: v => $"value:{v}",
            onFailure: e => $"error:{e.Code}"
        );

        var failureResult = failure.Match(
            onSuccess: v => $"value:{v}",
            onFailure: e => $"error:{e.Code}"
        );

        Assert.Equal("value:10", successResult);
        Assert.Equal("error:err", failureResult);
    }

    [Fact]
    public void Success_AllowsNullForNullableTypes()
    {
        // Result<T?> should allow null values for nullable types
        // This is needed for operations like AverageAsync which returns null for empty collections
        var result = Result<string?>.Success(null);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }

    [Fact]
    public void Failure_ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => Result<int>.Failure(null!));
    }
}
