using System.Text;

namespace Fmc.Api.Middleware;

/// <summary>Protege /swagger con basic auth cuando FMC_SWAGGER_USER y FMC_SWAGGER_PASSWORD están definidos.</summary>
public class SwaggerBasicAuthMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var user = Environment.GetEnvironmentVariable("FMC_SWAGGER_USER");
        var pass = Environment.GetEnvironmentVariable("FMC_SWAGGER_PASSWORD");

        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass)
            || !context.Request.Path.StartsWithSegments("/swagger"))
        {
            await next(context);
            return;
        }

        var header = context.Request.Headers.Authorization.ToString();
        if (TryValidateBasicAuth(header, user, pass))
        {
            await next(context);
            return;
        }

        context.Response.Headers.WWWAuthenticate = "Basic realm=\"FMC Swagger\"";
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
    }

    private static bool TryValidateBasicAuth(string? header, string expectedUser, string expectedPass)
    {
        if (string.IsNullOrEmpty(header) || !header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            return false;

        try
        {
            var encoded = header["Basic ".Length..].Trim();
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            var sep = decoded.IndexOf(':');
            if (sep < 0) return false;
            var u = decoded[..sep];
            var p = decoded[(sep + 1)..];
            return u == expectedUser && p == expectedPass;
        }
        catch
        {
            return false;
        }
    }
}

public static class SwaggerBasicAuthMiddlewareExtensions
{
    public static IApplicationBuilder UseSwaggerBasicAuth(this IApplicationBuilder app) =>
        app.UseMiddleware<SwaggerBasicAuthMiddleware>();
}
