namespace FireBlazor.Tests.AILogic;

public class SafetySettingsTests
{
    [Fact]
    public void SafetySetting_BlockMedium_CreatesCorrectSetting()
    {
        // Act
        var setting = SafetySetting.BlockMedium(HarmCategory.HateSpeech);

        // Assert
        Assert.Equal(HarmCategory.HateSpeech, setting.Category);
        Assert.Equal(HarmBlockThreshold.BlockMediumAndAbove, setting.Threshold);
    }

    [Fact]
    public void SafetySetting_BlockHigh_CreatesCorrectSetting()
    {
        // Act
        var setting = SafetySetting.BlockHigh(HarmCategory.DangerousContent);

        // Assert
        Assert.Equal(HarmCategory.DangerousContent, setting.Category);
        Assert.Equal(HarmBlockThreshold.BlockOnlyHigh, setting.Threshold);
    }

    [Fact]
    public void SafetySetting_NoBlock_CreatesCorrectSetting()
    {
        // Act
        var setting = SafetySetting.NoBlock(HarmCategory.Harassment);

        // Assert
        Assert.Equal(HarmCategory.Harassment, setting.Category);
        Assert.Equal(HarmBlockThreshold.BlockNone, setting.Threshold);
    }

    [Fact]
    public void SafetyRating_IsProbabilityAtLeast_ReturnsCorrectResult()
    {
        // Arrange
        var rating = new SafetyRating
        {
            Category = HarmCategory.HateSpeech,
            Probability = HarmProbability.Medium,
            Blocked = false
        };

        // Assert
        Assert.True(rating.IsProbabilityAtLeast(HarmProbability.Low));
        Assert.True(rating.IsProbabilityAtLeast(HarmProbability.Medium));
        Assert.False(rating.IsProbabilityAtLeast(HarmProbability.High));
    }

    [Fact]
    public void GenerateContentResponse_WasBlocked_ReturnsCorrectly()
    {
        // Arrange
        var responseNotBlocked = new GenerateContentResponse
        {
            Text = "Hello",
            SafetyRatings = new[]
            {
                new SafetyRating { Category = HarmCategory.HateSpeech, Probability = HarmProbability.Low, Blocked = false }
            }
        };

        var responseBlocked = new GenerateContentResponse
        {
            Text = "",
            SafetyRatings = new[]
            {
                new SafetyRating { Category = HarmCategory.HateSpeech, Probability = HarmProbability.High, Blocked = true }
            }
        };

        // Assert
        Assert.False(responseNotBlocked.WasBlocked);
        Assert.True(responseBlocked.WasBlocked);
    }

    [Fact]
    public void GenerateContentResponse_BlockingRatings_ReturnsBlockedOnly()
    {
        // Arrange
        var response = new GenerateContentResponse
        {
            Text = "",
            SafetyRatings = new[]
            {
                new SafetyRating { Category = HarmCategory.HateSpeech, Probability = HarmProbability.High, Blocked = true },
                new SafetyRating { Category = HarmCategory.Harassment, Probability = HarmProbability.Low, Blocked = false }
            }
        };

        // Act
        var blocking = response.BlockingRatings.ToList();

        // Assert
        Assert.Single(blocking);
        Assert.Equal(HarmCategory.HateSpeech, blocking[0].Category);
    }
}
