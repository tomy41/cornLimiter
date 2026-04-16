using Exceptionless;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CornLimiter.Middleware;

public class LoggerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LoggerMiddleware> _logger;

    public LoggerMiddleware(RequestDelegate next, ILogger<LoggerMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

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

        var sw = Stopwatch.StartNew();
        _logger.LogInformation("Request started: {Method} {Path} - CorrelationId:{CorrelationId}",
            context.Request.Method, context.Request.Path, correlationId);

        await _next(context);

        sw.Stop();
        _logger.LogInformation("Request finished: {Method} {Path} - Status:{StatusCode} - Elapsed:{Elapsed}ms - CorrelationId:{CorrelationId}",
            context.Request.Method, context.Request.Path, context.Response.StatusCode, sw.ElapsedMilliseconds, correlationId);

    }
}