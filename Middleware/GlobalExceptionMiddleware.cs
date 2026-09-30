using System.Net;
using System.Text.Json;
using RaigonArts.Api.Common;

namespace RaigonArts.Api.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public GlobalExceptionMiddleware(
        RequestDelegate _next,
        ILogger<GlobalExceptionMiddleware> logger,
        IWebHostEnvironment env)
    {
        this._next = _next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);

            if (context.Response.StatusCode == (int)HttpStatusCode.Unauthorized && !context.Response.HasStarted)
            {
                await HandleUnauthorizedAsync(context);
            }
            else if (context.Response.StatusCode == (int)HttpStatusCode.Forbidden && !context.Response.HasStarted)
            {
                await HandleForbiddenAsync(context);
            }
        }
        catch (ApiException apiEx)
        {
            _logger.LogWarning(apiEx, "Handled API exception: {Message}", apiEx.Message);
            await HandleApiExceptionAsync(context, apiEx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled server error: {Message}", ex.Message);
            await HandleGenericExceptionAsync(context, ex);
        }
    }

    private static async Task HandleApiExceptionAsync(HttpContext context, ApiException apiEx)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = apiEx.StatusCode;

        var response = ApiErrorResponse.Create(
            apiEx.StatusCode,
            apiEx.ErrorCode,
            apiEx.Message,
            apiEx.Errors
        );

        var json = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(json);
    }

    private async Task HandleGenericExceptionAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var errorMessage = _env.IsDevelopment()
            ? ex.Message
            : "An unexpected internal server error occurred. Please contact the workshop administrator.";

        var response = ApiErrorResponse.Create(
            (int)HttpStatusCode.InternalServerError,
            "INTERNAL_SERVER_ERROR",
            errorMessage
        );

        var json = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(json);
    }

    private static async Task HandleUnauthorizedAsync(HttpContext context)
    {
        context.Response.ContentType = "application/json";
        var response = ApiErrorResponse.Create(
            401,
            "UNAUTHORIZED",
            "Bearer token is missing, malformed, or expired."
        );
        var json = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(json);
    }

    private static async Task HandleForbiddenAsync(HttpContext context)
    {
        context.Response.ContentType = "application/json";
        var response = ApiErrorResponse.Create(
            403,
            "FORBIDDEN",
            "Insufficient permissions for the requested operation."
        );
        var json = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(json);
    }
}
