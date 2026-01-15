namespace FireBlazor.Tests.Core;

public class ResultExtensionsTests
{
    [Fact]
    public async Task OrThrow_ReturnsValue_OnSuccess()
    {
        var result = Task.FromResult(Result<int>.Success(42));

        var value = await result.OrThrow();

        Assert.Equal(42, value);
    }

    [Fact]
    public async Task OrThrow_ThrowsFirebaseException_OnFailure()
    {
        var error = new FirebaseError("auth/invalid-email", "Invalid email format");
        var result = Task.FromResult(Result<int>.Failure(error));

        var ex = await Assert.ThrowsAsync<FirebaseException>(() => result.OrThrow());

        Assert.Equal("auth/invalid-email", ex.Code);
        Assert.Equal("Invalid email format", ex.Message);
    }

    [Fact]
    public void OrThrow_Sync_ReturnsValue_OnSuccess()
    {
        var result = Result<string>.Success("hello");

        var value = result.OrThrow();

        Assert.Equal("hello", value);
    }

    [Fact]
    public void OrThrow_Sync_ThrowsFirebaseException_OnFailure()
    {
        var error = new FirebaseError("firestore/permission-denied", "Access denied");
        var result = Result<string>.Failure(error);

        var ex = Assert.Throws<FirebaseException>(() => result.OrThrow());

        Assert.Equal("firestore/permission-denied", ex.Code);
    }

    [Fact]
    public async Task OrThrow_ValueTask_ReturnsValue_OnSuccess()
    {
        var result = new ValueTask<Result<int>>(Result<int>.Success(42));

        var value = await result.OrThrow();

        Assert.Equal(42, value);
    }

    [Fact]
    public async Task OrThrow_ValueTask_ThrowsFirebaseException_OnFailure()
    {
        var error = new FirebaseError("test/error", "Test error");
        var result = new ValueTask<Result<int>>(Result<int>.Failure(error));

        var ex = await Assert.ThrowsAsync<FirebaseException>(() => result.OrThrow().AsTask());

        Assert.Equal("test/error", ex.Code);
    }
}
