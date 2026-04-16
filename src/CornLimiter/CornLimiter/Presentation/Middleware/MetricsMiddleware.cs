using System.Diagnostics;

namespace CornLimiter.Presentation.Middleware
{
    public class MetricsMiddleware(RequestDelegate next, ILogger<LoggerMiddleware> logger)
    {
        private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));
        private readonly ILogger<LoggerMiddleware> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public async Task InvokeAsync(HttpContext context)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            var correlationId = context.TraceIdentifier;
            context.Response.OnStarting(() =>
            {
                if (!context.Response.Headers.ContainsKey("X-Correlation-ID"))
                {
                    context.Response.Headers["X-Correlation-ID"] = correlationId;
                }
                return Task.CompletedTask;
            });

            var sw = Stopwatch.StartNew();

            await _next(context);

            sw.Stop();

            // For demo purposes only we send the metrics to the logger. In a real application, you would send this to a monitoring system.
            _logger.LogInformation("Request: {Method} {Path} - CorrelationId:{CorrelationId} - ElapsedMilliseconds:{ElapsedMilliseconds}",
            context.Request.Method, context.Request.Path, correlationId, sw.ElapsedMilliseconds);
        }
    }
}
