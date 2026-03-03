using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Planeroo.Application.Behaviors;

/// <summary>
/// Logs all requests with timing for performance monitoring.
/// </summary>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("[Planeroo] Handling {RequestName}", requestName);

        var sw = Stopwatch.StartNew();
        var response = await next();
        sw.Stop();

        if (sw.ElapsedMilliseconds > 500)
        {
            _logger.LogWarning("[Planeroo] Long running request: {RequestName} ({ElapsedMs}ms)", requestName, sw.ElapsedMilliseconds);
        }
        else
        {
            _logger.LogInformation("[Planeroo] Handled {RequestName} in {ElapsedMs}ms", requestName, sw.ElapsedMilliseconds);
        }

        return response;
    }
}
