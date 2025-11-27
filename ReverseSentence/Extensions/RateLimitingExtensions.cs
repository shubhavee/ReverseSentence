using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace ReverseSentence.Extensions;

/// <summary>
/// Extension methods for configuring rate limiting
/// </summary>
public static class RateLimitingExtensions
{
    /// <summary>
    /// Adds rate limiting services with configured policies
    /// </summary>
    public static IServiceCollection AddRateLimitingPolicies(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRateLimiter(options =>
        {
            // General API endpoints - Token Bucket (allows bursts)
            options.AddTokenBucketLimiter("api-limit", config =>
            {
                config.TokenLimit = configuration.GetValue<int>("RateLimiting:Api:TokenLimit", 100);
                config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                config.QueueLimit = 5;
                config.ReplenishmentPeriod = TimeSpan.FromMinutes(configuration.GetValue<int>("RateLimiting:Api:ReplenishmentMinutes", 1));
                config.TokensPerPeriod = configuration.GetValue<int>("RateLimiting:Api:TokensPerPeriod", 100);
                config.AutoReplenishment = true;
            });

            // Auth endpoints (login) - Token Bucket for burst protection
            options.AddTokenBucketLimiter("auth-limit", config =>
            {
                config.TokenLimit = configuration.GetValue<int>("RateLimiting:Auth:TokenLimit", 10);
                config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                config.QueueLimit = 2;
                config.ReplenishmentPeriod = TimeSpan.FromMinutes(configuration.GetValue<int>("RateLimiting:Auth:ReplenishmentMinutes", 1));
                config.TokensPerPeriod = configuration.GetValue<int>("RateLimiting:Auth:TokensPerPeriod", 10);
                config.AutoReplenishment = true;
            });

            // Global sliding window
            options.AddSlidingWindowLimiter("global-limit", config =>
            {
                config.PermitLimit = configuration.GetValue<int>("RateLimiting:Global:PermitLimit", 100000);
                config.Window = TimeSpan.FromHours(configuration.GetValue<int>("RateLimiting:Global:WindowHours", 1));
                config.SegmentsPerWindow = 6;
                config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                config.QueueLimit = 10;
            });

            // Partition by IP address
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetNoLimiter(ipAddress);
            });

            // Custom 429 response
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";

                var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue)
                    ? retryAfterValue.TotalSeconds
                    : 60;

                context.HttpContext.Response.Headers["Retry-After"] = retryAfter.ToString();

                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    error = "Too many requests",
                    message = $"Rate limit exceeded. Please try again in {retryAfter} seconds.",
                    retryAfter = retryAfter
                }, cancellationToken);
            };
        });

        return services;
    }
}
