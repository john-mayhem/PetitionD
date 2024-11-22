using Microsoft.Extensions.Logging;

namespace PetitionD.Infrastructure.Resilience;

public enum CircuitState
{
    Closed,      // Normal operation
    Open,        // Failing, reject immediately
    HalfOpen     // Testing if service is back
}

public class CircuitBreaker
{
    private readonly ILogger<CircuitBreaker> _logger;
    private readonly int _failureThreshold;
    private readonly TimeSpan _resetTimeout;
    private readonly object _lock = new();

    private CircuitState _state;
    private int _failureCount;
    private DateTime? _lastFailureTime;
    private DateTime? _openTime;

    public CircuitBreaker(
        ILogger<CircuitBreaker> logger,
        int failureThreshold = 5,
        int resetTimeoutSeconds = 60)
    {
        _logger = logger;
        _failureThreshold = failureThreshold;
        _resetTimeout = TimeSpan.FromSeconds(resetTimeoutSeconds);
        _state = CircuitState.Closed;
    }

    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        await CheckStateAsync();

        try
        {
            if (_state == CircuitState.Open)
            {
                _logger.LogWarning("Circuit breaker is open, rejecting request");
                throw new CircuitBreakerOpenException();
            }

            var result = await action(cancellationToken);

            OnSuccess();
            return result;
        }
        catch (Exception ex)
        {
            OnFailure(ex);
            throw;
        }
    }

    private async Task CheckStateAsync()
    {
        if (_state != CircuitState.Open)
            return;

        if (_openTime == null)
            return;

        if (DateTime.UtcNow - _openTime.Value > _resetTimeout)
        {
            lock (_lock)
            {
                if (_state == CircuitState.Open)
                {
                    _logger.LogInformation("Circuit breaker moving to half-open state");
                    _state = CircuitState.HalfOpen;
                }
            }
        }
        else
        {
            await Task.Delay(100); // Small delay to prevent tight loop
        }
    }

    private void OnSuccess()
    {
        lock (_lock)
        {
            _failureCount = 0;
            _lastFailureTime = null;
            _openTime = null;

            if (_state == CircuitState.HalfOpen)
            {
                _logger.LogInformation("Circuit breaker recovered, closing circuit");
                _state = CircuitState.Closed;
            }
        }
    }

    private void OnFailure(Exception ex)
    {
        lock (_lock)
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;

            if (_failureCount >= _failureThreshold)
            {
                _logger.LogError(ex,
                    "Circuit breaker failure threshold reached ({Count}), opening circuit",
                    _failureCount);
                _state = CircuitState.Open;
                _openTime = DateTime.UtcNow;
            }
        }
    }
}

public class CircuitBreakerOpenException : Exception
{
    public CircuitBreakerOpenException()
        : base("Circuit breaker is open, request rejected") { }
}