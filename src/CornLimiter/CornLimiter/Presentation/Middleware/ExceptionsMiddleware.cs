using Exceptionless;
using System.Net;
using System.Text.Json;

namespace CornLimiter.Presentation.Middleware;

public class ExceptionsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionsMiddleware> _logger;

    public ExceptionsMiddleware(RequestDelegate next, ILogger<ExceptionsMiddleware> logger)
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

            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var result = JsonSerializer.Serialize(new
        {
            error = "Internal Server Error",
            detail = "An unexpected error occurred. Please try again later."
        });
        return context.Response.WriteAsync(result);
    }
}