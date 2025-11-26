using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using ReverseSentence.Services;

namespace ReverseSentence.Tests.Services;

public class InMemoryCacheTests
{
    private readonly IMemoryCache memoryCache;
    private readonly Mock<ILogger<InMemoryCache>> mockLogger;
    private readonly InMemoryCache cache;

    public InMemoryCacheTests()
    {
        memoryCache = new MemoryCache(new MemoryCacheOptions());
        mockLogger = new Mock<ILogger<InMemoryCache>>();
        cache = new InMemoryCache(memoryCache, mockLogger.Object);
    }

    [Fact]
    public async Task GetAsync_WithExistingKey_ShouldReturnValue()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";
        await cache.SetAsync(key, value, TimeSpan.FromMinutes(5));

        // Act
        var result = await cache.GetAsync<string>(key);

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public async Task GetAsync_WithNonExistingKey_ShouldReturnDefault()
    {
        // Arrange
        var key = "non-existing-key";

        // Act
        var result = await cache.GetAsync<string>(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_WithValue_ShouldStoreValue()
    {
        // Arrange
        var key = "test-key";
        var value = new { Name = "Test", Age = 30 };

        // Act
        await cache.SetAsync(key, value, TimeSpan.FromMinutes(5));
        var result = await cache.GetAsync<object>(key);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SetAsync_WithDefaultExpiration_ShouldUseOneHour()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";

        // Act
        await cache.SetAsync(key, value);
        var result = await cache.GetAsync<string>(key);

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public async Task RemoveAsync_WithExistingKey_ShouldRemoveValue()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";
        await cache.SetAsync(key, value, TimeSpan.FromMinutes(5));

        // Act
        await cache.RemoveAsync(key);
        var result = await cache.GetAsync<string>(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_WithNonExistingKey_ShouldNotThrow()
    {
        // Arrange
        var key = "non-existing-key";

        // Act
        var act = async () => await cache.RemoveAsync(key);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetAsync_AfterExpiration_ShouldReturnDefault()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";
        await cache.SetAsync(key, value, TimeSpan.FromMilliseconds(100));

        // Act
        await Task.Delay(200); // Wait for expiration
        var result = await cache.GetAsync<string>(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_UpdateExistingKey_ShouldOverwriteValue()
    {
        // Arrange
        var key = "test-key";
        var value1 = "first-value";
        var value2 = "second-value";

        // Act
        await cache.SetAsync(key, value1, TimeSpan.FromMinutes(5));
        await cache.SetAsync(key, value2, TimeSpan.FromMinutes(5));
        var result = await cache.GetAsync<string>(key);

        // Assert
        result.Should().Be(value2);
    }

    [Fact]
    public async Task GetAsync_WithComplexObject_ShouldReturnCorrectType()
    {
        // Arrange
        var key = "test-key";
        var value = new TestDto { Id = 1, Name = "Test" };

        // Act
        await cache.SetAsync(key, value, TimeSpan.FromMinutes(5));
        var result = await cache.GetAsync<TestDto>(key);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Test");
    }

    private class TestDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}

