namespace FireBlazor.Tests.AILogic;

public class GroundingTests
{
    [Fact]
    public void GroundingConfig_WithGoogleSearch_EnablesGrounding()
    {
        // Act
        var config = GroundingConfig.WithGoogleSearch();

        // Assert
        Assert.True(config.GoogleSearchGrounding);
        Assert.Null(config.DynamicRetrievalConfig);
    }

    [Fact]
    public void GroundingConfig_WithGoogleSearchAndThreshold_SetsConfig()
    {
        // Act
        var config = GroundingConfig.WithGoogleSearch(0.5f);

        // Assert
        Assert.True(config.GoogleSearchGrounding);
        Assert.NotNull(config.DynamicRetrievalConfig);
        Assert.Equal(DynamicRetrievalMode.Dynamic, config.DynamicRetrievalConfig.Mode);
        Assert.Equal(0.5f, config.DynamicRetrievalConfig.DynamicThreshold);
    }

    [Fact]
    public void GenerateContentResponse_IsGrounded_ReturnsTrueWithChunks()
    {
        // Arrange
        var response = new GenerateContentResponse
        {
            Text = "Grounded response",
            GroundingMetadata = new GroundingMetadata
            {
                GroundingChunks = new[]
                {
                    new GroundingChunk { Web = new WebSource { Uri = "https://example.com", Title = "Example" } }
                }
            }
        };

        // Assert
        Assert.True(response.IsGrounded);
    }

    [Fact]
    public void GenerateContentResponse_IsGrounded_ReturnsFalseWithoutChunks()
    {
        // Arrange
        var response = new GenerateContentResponse
        {
            Text = "Ungrounded response"
        };

        // Assert
        Assert.False(response.IsGrounded);
    }

    [Fact]
    public void GenerateContentResponse_GroundingSources_ReturnsWebSources()
    {
        // Arrange
        var response = new GenerateContentResponse
        {
            Text = "Grounded response",
            GroundingMetadata = new GroundingMetadata
            {
                GroundingChunks = new[]
                {
                    new GroundingChunk { Web = new WebSource { Uri = "https://a.com", Title = "A" } },
                    new GroundingChunk { Web = new WebSource { Uri = "https://b.com", Title = "B" } }
                }
            }
        };

        // Act
        var sources = response.GroundingSources.ToList();

        // Assert
        Assert.Equal(2, sources.Count);
        Assert.Equal("https://a.com", sources[0].Uri);
        Assert.Equal("https://b.com", sources[1].Uri);
    }

    [Fact]
    public void GroundingMetadata_WithSearchQueries_StoresQueries()
    {
        // Arrange
        var metadata = new GroundingMetadata
        {
            SearchQueries = new[] { "query 1", "query 2" }
        };

        // Assert
        Assert.Equal(2, metadata.SearchQueries!.Count);
        Assert.Equal("query 1", metadata.SearchQueries[0]);
    }

    [Fact]
    public void GroundingSupport_LinksSegmentToChunks()
    {
        // Arrange
        var support = new GroundingSupport
        {
            Segment = new GroundingSegment
            {
                StartIndex = 0,
                EndIndex = 50,
                Text = "This is the grounded text"
            },
            GroundingChunkIndices = new[] { 0, 2 },
            ConfidenceScores = new[] { 0.9f, 0.7f }
        };

        // Assert
        Assert.Equal(0, support.Segment!.StartIndex);
        Assert.Equal(50, support.Segment.EndIndex);
        Assert.Equal(2, support.GroundingChunkIndices!.Count);
        Assert.Equal(0.9f, support.ConfidenceScores![0]);
    }
}
