using Microsoft.Extensions.Hosting;

namespace Midori.Database;

public interface IDatabaseProvider : IHostedService
{
    IDatabaseTable<T> GetTable<T>(string name);
}
