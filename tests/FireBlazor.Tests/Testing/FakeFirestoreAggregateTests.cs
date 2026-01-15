using FireBlazor.Testing;

namespace FireBlazor.Tests.Testing;

/// <summary>
/// Tests for FakeFirestore aggregate query support.
/// </summary>
public class FakeFirestoreAggregateTests
{
    private readonly FakeFirestore _firestore = new();

    public class Order
    {
        public string? Id { get; set; }
        public decimal Amount { get; set; }
        public string? Status { get; set; }
    }

    [Fact]
    public async Task CountAsync_ReturnsDocumentCount()
    {
        // Arrange
        _firestore.SeedData("orders", new[]
        {
            new Order { Id = "1", Amount = 100 },
            new Order { Id = "2", Amount = 200 },
            new Order { Id = "3", Amount = 300 }
        });

        // Act
        var result = await _firestore.Collection<Order>("orders").Aggregate().CountAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value);
    }

    [Fact]
    public async Task SumAsync_ReturnsSumOfField()
    {
        // Arrange
        _firestore.SeedData("orders", new[]
        {
            new Order { Id = "1", Amount = 100 },
            new Order { Id = "2", Amount = 200 },
            new Order { Id = "3", Amount = 300 }
        });

        // Act
        var result = await _firestore.Collection<Order>("orders").Aggregate().SumAsync(nameof(Order.Amount));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(600, result.Value);
    }

    [Fact]
    public async Task AverageAsync_ReturnsAverageOfField()
    {
        // Arrange
        _firestore.SeedData("orders", new[]
        {
            new Order { Id = "1", Amount = 100 },
            new Order { Id = "2", Amount = 200 },
            new Order { Id = "3", Amount = 300 }
        });

        // Act
        var result = await _firestore.Collection<Order>("orders").Aggregate().AverageAsync(nameof(Order.Amount));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.Value);
    }

    [Fact]
    public async Task AverageAsync_WithEmptyCollection_ReturnsNull()
    {
        // Act
        var result = await _firestore.Collection<Order>("orders").Aggregate().AverageAsync(nameof(Order.Amount));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task CountAsync_WithSimulatedError_ReturnsFailure()
    {
        // Arrange
        _firestore.SimulateError(new FirebaseError("test-code", "Test error"));

        // Act
        var result = await _firestore.Collection<Order>("orders").Aggregate().CountAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("test-code", result.Error!.Code);
    }

    [Fact]
    public async Task CountAsync_WithEmptyCollection_ReturnsZero()
    {
        // Act
        var result = await _firestore.Collection<Order>("orders").Aggregate().CountAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value);
    }

    [Fact]
    public async Task SumAsync_WithEmptyCollection_ReturnsZero()
    {
        // Act
        var result = await _firestore.Collection<Order>("orders").Aggregate().SumAsync(nameof(Order.Amount));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value);
    }
}
