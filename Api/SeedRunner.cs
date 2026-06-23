using Fmc.Application.Configuration;
using Fmc.Infrastructure;
using Fmc.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fmc.Api;

/// <summary>Ejecuta migraciones + seed sin levantar el servidor HTTP (<c>make seed</c>).</summary>
public static class SeedRunner
{
    public static async Task<int> RunAsync(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Services.AddInfrastructure(builder.Configuration);

        var app = builder.Build();

        var media = builder.Configuration.GetSection(MediaOptions.SectionName).Get<MediaOptions>()
                    ?? new MediaOptions();

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();
        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
        await db.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout=5000;");

        var count = await DataSeeder.EnsureCabaCatalogAsync(db, media);

        Console.WriteLine($"Seed OK: {count} cafeterías en CABA (contraseña demo: {DataSeeder.DemoPassword}).");
        Console.WriteLine($"  BD: {db.Database.GetConnectionString()}");
        Console.WriteLine($"  Media: {System.IO.Path.GetFullPath(media.UploadRoot)}");
        return 0;
    }
}
