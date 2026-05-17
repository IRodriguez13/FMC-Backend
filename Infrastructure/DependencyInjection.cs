using Fmc.Application.Interfaces;
using Fmc.Infrastructure.Auth;
using Fmc.Infrastructure.Persistence;
using Fmc.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fmc.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(o =>
            o.UseSqlite(configuration.GetConnectionString("Default") ?? "Data Source=fmc.db"));

        services.AddScoped<ICafeteriaRepository, CafeteriaRepository>();
        services.AddScoped<IConsumerUserRepository, ConsumerUserRepository>();
        services.AddScoped<IEnterpriseUserRepository, EnterpriseUserRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        return services;
    }
}
