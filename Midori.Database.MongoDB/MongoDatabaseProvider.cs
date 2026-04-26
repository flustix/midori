using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;

namespace Midori.Database.MongoDB;

public class MongoDatabaseProvider : IDatabaseProvider
{
    private readonly MongoClient client;
    private readonly IMongoDatabase db;

    public MongoDatabaseProvider(IOptions<MongoConfig> settings, ILoggerFactory loggerFactory)
    {
        var mcs = MongoClientSettings.FromConnectionString(settings.Value.Connection);
        mcs.LoggingSettings = new LoggingSettings(loggerFactory);
        client = new MongoClient(mcs);

        db = client.GetDatabase(settings.Value.Database);
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken)
    {
        client.Dispose();
        return Task.CompletedTask;
    }

    public IDatabaseTable<T> GetTable<T>(string name)
        => new MongoDatabaseTable<T>(db, name);
}
