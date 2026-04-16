using System;
using System.Threading.Tasks;
using Exceptionless;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CornLimiter.Middleware;

public class ExceptionlessMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionlessMiddleware> _logger;

    public ExceptionlessMiddleware(RequestDelegate next, ILogger<ExceptionlessMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception caught by ExceptionlessMiddleware.");

            try
            {
                ex.ToExceptionless()?.Submit();
            }
            catch (Exception sendEx)
            {
                _logger.LogWarning(sendEx, "Failed to report exception to Exceptionless.");
            }
            throw;
        }
    }
}