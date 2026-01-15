namespace FireBlazor.Tests.AILogic;

public class ChatMessageTests
{
    [Fact]
    public void ChatMessage_WithUserRole_SetsPropertiesCorrectly()
    {
        var message = new ChatMessage { Role = "user", Content = "Hello" };

        Assert.Equal("user", message.Role);
        Assert.Equal("Hello", message.Content);
    }

    [Fact]
    public void ChatMessage_WithModelRole_SetsPropertiesCorrectly()
    {
        var message = new ChatMessage { Role = "model", Content = "Hi there!" };

        Assert.Equal("model", message.Role);
        Assert.Equal("Hi there!", message.Content);
    }

    [Fact]
    public void ChatMessage_Equality_WorksCorrectly()
    {
        var message1 = new ChatMessage { Role = "user", Content = "Hello" };
        var message2 = new ChatMessage { Role = "user", Content = "Hello" };

        Assert.Equal(message1, message2);
    }

    [Fact]
    public void ChatMessage_Inequality_WhenRoleDiffers()
    {
        var message1 = new ChatMessage { Role = "user", Content = "Hello" };
        var message2 = new ChatMessage { Role = "model", Content = "Hello" };

        Assert.NotEqual(message1, message2);
    }

    [Fact]
    public void ChatMessage_Inequality_WhenContentDiffers()
    {
        var message1 = new ChatMessage { Role = "user", Content = "Hello" };
        var message2 = new ChatMessage { Role = "user", Content = "Hi" };

        Assert.NotEqual(message1, message2);
    }
}
