using FluentAssertions;
using System.Threading.RateLimiting;

namespace ReverseSentence.Tests.Services;

/// <summary>
/// Unit tests for rate limiting configuration and policies
/// Tests rate limiter behavior in isolation without HTTP overhead
/// Uses small time windows for fast test execution
/// </summary>
public class RateLimitingTests
{
    [Fact]
    public async Task FixedWindowLimiter_ShouldAllowUpToPermitLimit()
    {
        // Arrange - Small window for fast testing
        var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromSeconds(10),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0 // No queue for this test
        });

        var acquiredCount = 0;

        // Act - Try to acquire 7 permits rapidly
        for (int i = 0; i < 7; i++)
        {
            using var lease = await limiter.AcquireAsync(permitCount: 1);
            if (lease.IsAcquired) acquiredCount++;
        }

        // Assert
        acquiredCount.Should().Be(5, "Fixed window should allow exactly 5 permits");
    }

    [Fact]
    public async Task TokenBucketLimiter_ShouldEnforceTokenLimit()
    {
        // Arrange - Small limits for fast testing
        var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = 5,
            ReplenishmentPeriod = TimeSpan.FromSeconds(10),
            TokensPerPeriod = 5,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });

        // Act - Exhaust all tokens
        var acquiredCount = 0;
        for (int i = 0; i < 7; i++)
        {
            using var lease = await limiter.AcquireAsync(permitCount: 1);
            if (lease.IsAcquired) acquiredCount++;
        }

        // Assert
        acquiredCount.Should().Be(5, "Should acquire exactly 5 tokens");
    }

    [Fact]
    public async Task SlidingWindowLimiter_ShouldEnforceSlidingTimeWindow()
    {
        // Arrange - Smaller limits for fast testing
        var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromSeconds(1),
            SegmentsPerWindow = 4,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });

        var acquiredCount = 0;

        // Act - Try to acquire 25 permits
        for (int i = 0; i < 25; i++)
        {
            using var lease = await limiter.AcquireAsync(permitCount: 1);
            if (lease.IsAcquired) acquiredCount++;
        }

        // Assert
        acquiredCount.Should().Be(20, "Should allow exactly 20 permits within window");
    }

    [Fact]
    public async Task RateLimiter_WhenExhausted_ShouldProvideRetryAfter()
    {
        // Arrange
        var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            PermitLimit = 3,
            Window = TimeSpan.FromSeconds(5),
            QueueLimit = 0
        });

        // Act - Exhaust all permits
        for (int i = 0; i < 3; i++)
        {
            await limiter.AcquireAsync(permitCount: 1);
        }

        // Try one more (should be rejected)
        using var lease = await limiter.AcquireAsync(permitCount: 1);

        // Assert
        lease.IsAcquired.Should().BeFalse("Rate limit exhausted");
        lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter).Should().BeTrue("Should provide retry-after");
        retryAfter.Should().BeGreaterThan(TimeSpan.Zero, "Retry-after should be positive");
        retryAfter.Should().BeLessThanOrEqualTo(TimeSpan.FromSeconds(5), "Retry-after should be within window");
    }

    [Fact]
    public async Task FixedWindowLimiter_AfterWindowExpires_ShouldResetPermits()
    {
        // Arrange - Very small window for fast testing
        var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            PermitLimit = 2,
            Window = TimeSpan.FromMilliseconds(100),
            QueueLimit = 0
        });

        // Act - Exhaust permits
        for (int i = 0; i < 2; i++)
        {
            var lease = await limiter.AcquireAsync(permitCount: 1);
            lease.IsAcquired.Should().BeTrue();
        }

        // Verify exhausted
        using (var lease = await limiter.AcquireAsync(permitCount: 1))
        {
            lease.IsAcquired.Should().BeFalse("Should be exhausted");
        }

        // Wait for window to reset
        await Task.Delay(TimeSpan.FromMilliseconds(150));

        // Try again - should work
        using (var lease = await limiter.AcquireAsync(permitCount: 1))
        {
            lease.IsAcquired.Should().BeTrue("Window should have reset");
        }
    }

    [Fact]
    public async Task ConcurrentAcquisition_ShouldBeThreadSafe()
    {
        // Arrange
        var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromSeconds(1),
            QueueLimit = 0
        });

        var acquiredCount = 0;
        var lockObj = new object();

        // Act - 20 concurrent acquisition attempts
        var tasks = Enumerable.Range(1, 20).Select(async i =>
        {
            using var lease = await limiter.AcquireAsync(permitCount: 1);
            if (lease.IsAcquired)
            {
                lock (lockObj)
                {
                    acquiredCount++;
                }
            }
        });

        await Task.WhenAll(tasks);

        // Assert
        acquiredCount.Should().Be(10, "Should be thread-safe and allow exactly 10 permits");
    }

    [Fact]
    public void RateLimiterOptions_ShouldValidateConfiguration()
    {
        // Act & Assert - Permit limit must be positive
        Action act = () => new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            PermitLimit = 0, // Invalid
            Window = TimeSpan.FromMinutes(1)
        });

        act.Should().Throw<ArgumentException>("PermitLimit must be > 0");
    }

    [Fact]
    public async Task QueueLimit_WhenExceeded_ShouldRejectImmediately()
    {
        // Arrange - Limiter with small queue
        var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            PermitLimit = 3,
            Window = TimeSpan.FromSeconds(2),
            QueueLimit = 0 // No queue
        });

        // Act - Try to acquire more than permit limit
        var acquiredCount = 0;
        var rejectedCount = 0;
        
        for (int i = 0; i < 5; i++)
        {
            using var lease = await limiter.AcquireAsync(permitCount: 1);
            if (lease.IsAcquired) acquiredCount++;
            else rejectedCount++;
        }

        // Assert
        acquiredCount.Should().Be(3, "Should acquire exactly 3 permits");
        rejectedCount.Should().Be(2, "Requests beyond limit should be rejected immediately");
    }
}
