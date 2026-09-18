using System.Net;
using System.Text.Json;
using AccessRequestHub.Exceptions;

namespace AccessRequestHub.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = HttpStatusCode.InternalServerError; // 500
        var message = "An unexpected error occurred.";

        // Map our custom exceptions to HTTP Status Codes
        switch (exception)
        {
            case NotFoundException nfEx:
                statusCode = HttpStatusCode.NotFound; // 404
                message = nfEx.Message;
                break;
            case ForbiddenException fbEx:
                statusCode = HttpStatusCode.Forbidden; // 403
                message = fbEx.Message;
                break;
            case ConflictException cfEx:
                statusCode = HttpStatusCode.Conflict; // 409
                message = cfEx.Message;
                break;
            case BusinessException bsEx:
                statusCode = HttpStatusCode.BadRequest; // 400
                message = bsEx.Message;
                break;
            default:
                _logger.LogError(exception, "Unhandled exception occurred");
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var result = JsonSerializer.Serialize(new { error = message, statusCode = (int)statusCode });
        return context.Response.WriteAsync(result);
    }
}