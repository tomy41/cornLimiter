namespace CornLimiter.Presentation.Middleware;

public class LoggerMiddleware(RequestDelegate next, ILogger<LoggerMiddleware> logger)
{
    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));
    private readonly ILogger<LoggerMiddleware> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task InvokeAsync(HttpContext context)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        var correlationId = context.TraceIdentifier;

        // Añade X-Correlation-ID en la respuesta si no fue provisto por el cliente
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey("X-Correlation-ID"))
            {
                context.Response.Headers["X-Correlation-ID"] = correlationId;
            }
            return Task.CompletedTask;
        });

        _logger.LogInformation("Request: {Method} {Path} - CorrelationId:{CorrelationId}",
            context.Request.Method, context.Request.Path, correlationId);

        await _next(context);

    }
}