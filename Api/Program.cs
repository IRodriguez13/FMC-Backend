using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Fmc.Api.Branding;
using Fmc.Api.Endpoints;
using Fmc.Api.GraphQL;
using Fmc.Api.Middleware;
using Fmc.Application;
using Fmc.Application.Configuration;
using Fmc.Infrastructure;
using Fmc.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.FileProviders;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Threading.RateLimiting;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{

if (args.Contains("--seed-only"))
{
    Environment.Exit(await Fmc.Api.SeedRunner.RunAsync(args));
}

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ─────────────────────────────────────────────────────────────
builder.Logging.ClearProviders();
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .Filter.ByExcluding(static e =>
        e.Exception is OperationCanceledException or TaskCanceledException));

// ── Capas ───────────────────────────────────────────────────────────────
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddFmcGraphQL();

// ── Opciones ────────────────────────────────────────────────────────────
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<DiscoveryOptions>(builder.Configuration.GetSection(DiscoveryOptions.SectionName));
builder.Services.Configure<MediaOptions>(builder.Configuration.GetSection(MediaOptions.SectionName));
builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection(CorsOptions.SectionName));
builder.Services.Configure<RateLimitingOptions>(builder.Configuration.GetSection(RateLimitingOptions.SectionName));
builder.Services.Configure<DemoOptions>(builder.Configuration.GetSection(DemoOptions.SectionName));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("db");

var rateLimit = builder.Configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>()
                ?? new RateLimitingOptions();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("auth", o =>
    {
        o.PermitLimit = rateLimit.AuthPermitLimit;
        o.Window = TimeSpan.FromSeconds(rateLimit.AuthWindowSeconds);
        o.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("upload", o =>
    {
        o.PermitLimit = rateLimit.UploadPermitLimit;
        o.Window = TimeSpan.FromSeconds(rateLimit.UploadWindowSeconds);
        o.QueueLimit = 0;
    });
});

builder.Services.AddCors(options =>
{
    var cors = builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new CorsOptions();
    options.AddPolicy("FmcCors", policy =>
    {
        if (cors.AllowedOrigins.Length > 0)
        {
            policy.WithOrigins(cors.AllowedOrigins)
                .WithMethods(cors.AllowedMethods)
                .WithHeaders(cors.AllowedHeaders);
        }
        else if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        }
    });
});

builder.Services.Configure<FormOptions>(o =>
{
    var media = builder.Configuration.GetSection(MediaOptions.SectionName).Get<MediaOptions>() ?? new MediaOptions();
    o.MultipartBodyLengthLimit = media.MaxFileSizeBytes;
});

// ── JSON ────────────────────────────────────────────────────────────────
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// ── JWT Authentication ──────────────────────────────────────────────────
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
          ?? throw new InvalidOperationException("Falta configuración Jwt.");

if (jwt.Key.Length < 32)
    throw new InvalidOperationException("Jwt:Key debe tener al menos 32 caracteres.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub,
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/json"]);
});

// ── Swagger ─────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = FmcApp.ApiTitle,
        Version = "v1",
        Description = FmcApp.ApiDescription,
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT: Authorization header `Bearer {token}`",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
            },
            Array.Empty<string>()
        },
    });
});

// ── Pipeline ────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseCorrelationId();
app.UseGlobalExceptionHandler();

var demoOptions = app.Configuration.GetSection(DemoOptions.SectionName).Get<DemoOptions>() ?? new DemoOptions();
var enableSwagger = app.Environment.IsDevelopment() || demoOptions.EnableSwagger;
if (enableSwagger)
{
    app.UseSwaggerBasicAuth();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Solo HTTP en dev/Docker; evita "Failed to determine the https port for redirect".
var disableHttpsRedirect =
    app.Environment.IsDevelopment()
    || Environment.GetEnvironmentVariable("FMC_DISABLE_HTTPS_REDIRECT") == "1";
if (!disableHttpsRedirect)
    app.UseHttpsRedirection();

app.UseResponseCompression();

var mediaOptions = app.Configuration.GetSection(MediaOptions.SectionName).Get<MediaOptions>() ?? new MediaOptions();
var uploadRootFull = System.IO.Path.GetFullPath(mediaOptions.UploadRoot);
Directory.CreateDirectory(uploadRootFull);

// PNG legacy con bytes JPEG rompían el navegador; redirigir a .jpg si existe.
app.Use(async (ctx, next) =>
{
    var path = ctx.Request.Path.Value ?? "";
    var mediaPrefix = mediaOptions.PublicUrlPath.TrimEnd('/');
    if (path.StartsWith(mediaPrefix + "/", StringComparison.OrdinalIgnoreCase))
    {
        var fileName = path[(mediaPrefix.Length + 1)..];
        if (fileName.StartsWith("seed-", StringComparison.OrdinalIgnoreCase)
            && fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
        {
            var jpgName = System.IO.Path.ChangeExtension(fileName, ".jpg");
            var jpgPath = System.IO.Path.Combine(uploadRootFull, jpgName);
            if (System.IO.File.Exists(jpgPath) && new System.IO.FileInfo(jpgPath).Length >= 1024)
            {
                ctx.Response.Redirect($"{mediaPrefix}/{jpgName}", permanent: true);
                return;
            }
        }
    }

    await next();
});

app.UseStaticFiles(new StaticFileOptions
{
    RequestPath = mediaOptions.PublicUrlPath,
    FileProvider = new PhysicalFileProvider(uploadRootFull),
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.CacheControl = "public,max-age=86400,immutable";
    },
});

app.UseCors("FmcCors");
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapFmcEndpoints();
app.MapFmcGraphQL();

if (app.Environment.IsDevelopment())
{
    app.MapGet("/", () => Results.Redirect("/swagger/index.html"))
        .AllowAnonymous()
        .ExcludeFromDescription();
}
else if (enableSwagger)
{
    app.MapGet("/", () => Results.Redirect("/swagger/index.html"))
        .AllowAnonymous()
        .ExcludeFromDescription();
}

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
    await db.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout=5000;");
    await DataSeeder.EnsureCabaCatalogAsync(db, mediaOptions);
}

await app.RunAsync();

}
catch (Exception ex)
{
    Log.Debug(ex, "La aplicación FMC terminó inesperadamente");
}
finally
{
    Log.CloseAndFlush();
}
