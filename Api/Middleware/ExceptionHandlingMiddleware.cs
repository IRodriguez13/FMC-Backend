using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Fmc.Api.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error no controlado");
            await WriteProblemAsync(context, ex);
        }
    }

    private static Task WriteProblemAsync(HttpContext context, Exception ex)
    {
        var (status, title, detail) = ex switch
        {
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "No autorizado", ex.Message),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "No encontrado", ex.Message),
            InvalidOperationException => (StatusCodes.Status409Conflict, "Conflicto", ex.Message),
            ArgumentException => (StatusCodes.Status400BadRequest, "Solicitud incorrecta", ex.Message),
            _ => (StatusCodes.Status500InternalServerError, "Error interno", "Se produjo un error inesperado."),
        };

        context.Response.ContentType = MediaTypeNames.Application.Json;
        context.Response.StatusCode = status;

        var problem = new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = status,
            Instance = context.Request.Path,
        };

        if (status == StatusCodes.Status500InternalServerError)
            problem.Detail = null;

        return context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app) =>
        app.UseMiddleware<ExceptionHandlingMiddleware>();
}
