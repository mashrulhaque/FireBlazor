namespace FireBlazor.Tests.AppCheck;

public class AppCheckTests
{
    [Fact]
    public void AppCheckToken_HasRequiredProperties()
    {
        var token = new AppCheckToken
        {
            Token = "test-token-123",
            ExpireTimeMillis = 1700000000000
        };

        Assert.Equal("test-token-123", token.Token);
        Assert.Equal(1700000000000, token.ExpireTimeMillis);
    }

    [Fact]
    public void AppCheckToken_ExpirationTime_CalculatesCorrectly()
    {
        var token = new AppCheckToken
        {
            Token = "test-token",
            ExpireTimeMillis = 1700000000000
        };

        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1700000000000), token.ExpirationTime);
    }

    [Fact]
    public void AppCheckToken_IsExpired_ReturnsFalseForFutureExpiry()
    {
        var futureTime = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeMilliseconds();
        var token = new AppCheckToken
        {
            Token = "test-token",
            ExpireTimeMillis = futureTime
        };

        Assert.False(token.IsExpired);
    }

    [Fact]
    public void AppCheckToken_IsExpired_ReturnsTrueForPastExpiry()
    {
        var pastTime = DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeMilliseconds();
        var token = new AppCheckToken
        {
            Token = "test-token",
            ExpireTimeMillis = pastTime
        };

        Assert.True(token.IsExpired);
    }
}
