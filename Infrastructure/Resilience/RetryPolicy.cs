namespace PetitionD.Infrastructure.Resilience;

public class RetryPolicy
{
    private readonly ILogger<RetryPolicy> _logger;
    private readonly int _maxRetries;
    private readonly TimeSpan _delay;
    private readonly double _exponentialBase;

    public RetryPolicy(
        ILogger<RetryPolicy> logger,
        int maxRetries = 3,
        int delayMilliseconds = 100,
        double exponentialBase = 2.0)
    {
        _logger = logger;
        _maxRetries = maxRetries;
        _delay = TimeSpan.FromMilliseconds(delayMilliseconds);
        _exponentialBase = exponentialBase;
    }

    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> action,
        Func<Exception, bool> shouldRetry,
        CancellationToken cancellationToken = default)
    {
        var retryCount = 0;
        var delay = _delay;

        while (true)
        {
            try
            {
                return await action(cancellationToken);
            }
            catch (Exception ex)
            {
                retryCount++;

                if (retryCount >= _maxRetries || !shouldRetry(ex))
                {
                    _logger.LogError(ex,
                        "Retry policy exhausted after {Count} attempts", retryCount);
                    throw;
                }

                _logger.LogWarning(ex,
                    "Operation failed, attempt {Count} of {Max}, retrying after {Delay}ms",
                    retryCount, _maxRetries, delay.TotalMilliseconds);

                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromMilliseconds(
                    delay.TotalMilliseconds * _exponentialBase);
            }
        }
    }
}