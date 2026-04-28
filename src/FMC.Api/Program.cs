using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Fmc.Api.Branding;
using Fmc.Api.Data;
using Fmc.Api.Endpoints;
using Fmc.Api.Middleware;
using Fmc.Api.Repositories;
using Fmc.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=fmc.db"));

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<DiscoveryOptions>(builder.Configuration.GetSection(DiscoveryOptions.SectionName));

builder.Services.AddScoped<IConsumerUserRepository, ConsumerUserRepository>();
builder.Services.AddScoped<IEnterpriseUserRepository, EnterpriseUserRepository>();
builder.Services.AddScoped<ICafeteriaRepository, CafeteriaRepository>();

builder.Services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

builder.Services.AddScoped<IConsumerAuthService, ConsumerAuthService>();
builder.Services.AddScoped<IEnterpriseAuthService, EnterpriseAuthService>();
builder.Services.AddScoped<IConsumerProfileService, ConsumerProfileService>();
builder.Services.AddScoped<IEnterpriseCafeteriaService, EnterpriseCafeteriaService>();
builder.Services.AddScoped<ICafeteriaDiscoveryService, CafeteriaDiscoveryService>();

builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

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

var app = builder.Build();

app.UseCorrelationId();
app.UseGlobalExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// En Docker suele usarse solo HTTP (8080); la redirección HTTPS rompe curl y Swagger en el host.
if (Environment.GetEnvironmentVariable("FMC_DISABLE_HTTPS_REDIRECT") != "1")
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();

app.MapFmcEndpoints();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await SeedIfEmptyAsync(db);
}

await app.RunAsync();

static async Task SeedIfEmptyAsync(AppDbContext db)
{
    if (await db.EnterpriseUsers.AnyAsync())
        return;

    const string plainPassword = "SeedPass-123";
    var hash = BCrypt.Net.BCrypt.HashPassword(plainPassword);

    // GUID fijos para que cualquier máquina con BD vacía obtenga los mismos IDs (útil para pruebas y documentación).
    var cafePremiumId = Guid.Parse("a1111111-1111-4111-8111-111111111101");
    var enterprisePremiumId = Guid.Parse("a1111111-1111-4111-8111-111111111201");
    var cafeStandardId = Guid.Parse("a2222222-2222-4222-8222-222222221101");
    var enterpriseStandardId = Guid.Parse("a2222222-2222-4222-8222-222222221201");
    var consumerFreeId = Guid.Parse("b3333333-3333-4333-8333-333333333301");
    var consumerPremiumId = Guid.Parse("b3333333-3333-4333-8333-333333333302");

    var cafePremium = new Cafeteria
    {
        Id = cafePremiumId,
        Name = "FMC Seed — Enterprise Premium",
        Description = "Demo: cuenta Enterprise Premium (mayor ponderación en listados).",
        Address = "Madrid (demo)",
        Latitude = 40.4169,
        Longitude = -3.7039,
        ListingActive = true,
        DiscountPercent = 15,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    var enterprisePremium = new EnterpriseUser
    {
        Id = enterprisePremiumId,
        Email = "enterprise-premium@seed.fmc",
        PasswordHash = hash,
        CafeteriaId = cafePremiumId,
        SubscriptionTier = EnterpriseSubscriptionTier.Premium,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    var cafeStandard = new Cafeteria
    {
        Id = cafeStandardId,
        Name = "FMC Seed — Enterprise Standard",
        Description = "Demo: cuenta Enterprise Standard (solo orden por distancia real).",
        Address = "Madrid (demo)",
        Latitude = 40.4174,
        Longitude = -3.7044,
        ListingActive = true,
        DiscountPercent = 8,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    var enterpriseStandard = new EnterpriseUser
    {
        Id = enterpriseStandardId,
        Email = "enterprise-standard@seed.fmc",
        PasswordHash = hash,
        CafeteriaId = cafeStandardId,
        SubscriptionTier = EnterpriseSubscriptionTier.Standard,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    var consumerFree = new ConsumerUser
    {
        Id = consumerFreeId,
        Email = "consumidor@seed.fmc",
        PasswordHash = hash,
        Tier = ConsumerTier.Free,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    var consumerPremium = new ConsumerUser
    {
        Id = consumerPremiumId,
        Email = "consumidor-premium@seed.fmc",
        PasswordHash = hash,
        Tier = ConsumerTier.Premium,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    db.Cafeterias.AddRange(cafePremium, cafeStandard);
    db.EnterpriseUsers.AddRange(enterprisePremium, enterpriseStandard);
    db.ConsumerUsers.AddRange(consumerFree, consumerPremium);
    await db.SaveChangesAsync();
}
