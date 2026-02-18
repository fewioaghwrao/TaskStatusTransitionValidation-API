
// ============================
// Infrastructure/ExceptionHandlingMiddleware.cs
// ============================
namespace TaskStatusTransitionValidation.Infrastructure;

using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TaskStatusTransitionValidation.Services;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException ex)
        {
            context.Response.ContentType = "application/problem+json; charset=utf-8";
            context.Response.StatusCode = ex.StatusCode;

            var pd = new ProblemDetails
            {
                Status = ex.StatusCode,
                Title = ex.Title ?? "Request failed",
                Detail = ex.Detail,
                Type = ex.Type ?? $"https://httpstatuses.com/{ex.StatusCode}"
            };

            if (ex.Extensions is not null)
            {
                foreach (var kv in ex.Extensions)
                    pd.Extensions[kv.Key] = kv.Value;
            }

            await context.Response.WriteAsync(JsonSerializer.Serialize(pd));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error");

            context.Response.ContentType = "application/problem+json; charset=utf-8";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            var pd = new ProblemDetails
            {
                Status = 500,
                Title = "Internal Server Error",
                Detail = "Unexpected error occurred.",
                Type = "https://httpstatuses.com/500"
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(pd));
        }
    }
}