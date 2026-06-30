using System.Net;
using System.Text.Json;
using ValiantXP.Domain.Exceptions;

namespace ValiantXP.API.Middleware;

/// <summary>
/// Global exception handler middleware.
/// Maps domain exceptions to appropriate HTTP status codes and structured JSON error responses.
/// Registered early in the pipeline so all unhandled exceptions are caught.
/// </summary>
public sealed class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AntiFraudException ex)
        {
            _logger.LogWarning("Anti-fraud rule fired: [{RuleCode}] {Message} | IP: {Ip}",
                ex.RuleCode, ex.Message, context.Connection.RemoteIpAddress);

            // Rate-limiting rule codes → 429, content violations → 422
            var statusCode = ex.RuleCode is "DAILY_LIMIT_EXCEEDED" or "IP_LIMIT_EXCEEDED" or "USER_BLOCKED"
                ? (int)HttpStatusCode.TooManyRequests
                : (int)HttpStatusCode.UnprocessableEntity;

            await WriteJsonResponse(context, statusCode, new
            {
                error = ex.Message,
                ruleCode = ex.RuleCode
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await WriteJsonResponse(context, (int)HttpStatusCode.InternalServerError, new
            {
                error = "An unexpected error occurred. Please try again later."
            });
        }
    }

    private static async Task WriteJsonResponse(HttpContext context, int statusCode, object body)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(body, _jsonOptions));
    }
}
