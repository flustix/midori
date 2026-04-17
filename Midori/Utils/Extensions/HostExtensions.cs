using Microsoft.Extensions.DependencyInjection;
using Midori.Networking;

namespace Midori.Utils.Extensions;

public static class HostExtensions
{
    public static IServiceCollection AddHttpServer(this IServiceCollection services, Action<HttpConfiguration>? config = null)
    {
        if (config != null)
            services.Configure(config);

        services.AddSingleton<HttpRouter>();
        services.AddHostedService<HttpServer>();
        return services;
    }
}
