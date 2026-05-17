

namespace Fmc.Api.GraphQL;

public static class GraphQLExtensions
{
    /// <summary>Registra el schema GraphQL de FMC (Hot Chocolate).</summary>
    public static IServiceCollection AddFmcGraphQL(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        services
            .AddGraphQLServer()
            .AddAuthorization()
            .AddQueryType<FmcQuery>();

        return services;
    }

    /// <summary>Mapea el endpoint GraphQL en /graphql (incluye Banana Cake Pop UI en Development).</summary>
    public static WebApplication MapFmcGraphQL(this WebApplication app)
    {
        app.MapGraphQL();
        return app;
    }
}
