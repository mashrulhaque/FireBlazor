namespace FireBlazor.Tests.Auth;

public class FirebaseUserTests
{
    [Fact]
    public void FirebaseUser_RequiredUid()
    {
        var user = new FirebaseUser
        {
            Uid = "abc123",
            Email = "test@example.com"
        };

        Assert.Equal("abc123", user.Uid);
        Assert.Equal("test@example.com", user.Email);
    }

    [Fact]
    public void FirebaseUser_DefaultValues()
    {
        var user = new FirebaseUser { Uid = "test" };

        Assert.Null(user.DisplayName);
        Assert.Null(user.PhotoUrl);
        Assert.False(user.IsEmailVerified);
        Assert.False(user.IsAnonymous);
        Assert.Empty(user.Providers);
    }
}
