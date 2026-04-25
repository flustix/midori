using Microsoft.Extensions.DependencyInjection;

namespace Midori.Database.MongoDB;

public static class MongoHostExtensions
{
    public static IServiceCollection AddMongoDatabase(this IServiceCollection services, string connection, string db, Action<MongoConfig>? config = null)
    {
        services.Configure<MongoConfig>(x =>
        {
            x.Connection = connection;
            x.Database = db;
            config?.Invoke(x);
        });

        services.AddSingleton<IDatabaseProvider, MongoDatabaseProvider>();
        return services;
    }
}
