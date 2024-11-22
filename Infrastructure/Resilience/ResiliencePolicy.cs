using Microsoft.Data.SqlClient;

namespace PetitionD.Infrastructure.Resilience;

public class ResiliencePolicy(
    ILogger<ResiliencePolicy> logger,
    CircuitBreaker circuitBreaker,
    RetryPolicy retryPolicy)
{
    private readonly CircuitBreaker _circuitBreaker = circuitBreaker;
    private readonly RetryPolicy _retryPolicy = retryPolicy;
    private readonly ILogger<ResiliencePolicy> _logger = logger;

    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> action,
        Func<Exception, bool> shouldRetry,
        CancellationToken cancellationToken = default)
    {
        return await _circuitBreaker.ExecuteAsync(async token =>
            await _retryPolicy.ExecuteAsync(action, shouldRetry, token),
            cancellationToken);
    }

    // Helper method for database operations
    public static bool ShouldRetryDatabaseOperation(Exception ex)
    {
        return ex is SqlException sqlEx && (
            // Deadlock
            sqlEx.Number == 1205 ||
            // Timeout
            sqlEx.Number == -2 ||
            // Connection issues
            sqlEx.Number == 53 ||
            sqlEx.Number == 1231 ||
            // Lock timeout
            sqlEx.Number == 1222
        );
    }
}