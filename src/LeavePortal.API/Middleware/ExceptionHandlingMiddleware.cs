using LeavePortal.Application.Common;

namespace LeavePortal.API.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (UnauthorizedAccessException ex) { await Write(context, StatusCodes.Status401Unauthorized, ex.Message); }
        catch (InvalidOperationException ex) { await Write(context, StatusCodes.Status400BadRequest, ex.Message); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled API exception");
            await Write(context, StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
        }
    }

    private static async Task Write(HttpContext context, int status, string message)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(message));
    }
}
