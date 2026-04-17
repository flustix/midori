using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Midori.Networking;

public partial class HttpRouter
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    private class TransientMethodModule<T> : IMethodModule
        where T : class, IHttpModule
    {
        public Action<T>? Configure { get; init; }

        public IHttpModule CreateHttpModule(IServiceProvider services)
        {
            var mod = ActivatorUtilities.CreateInstance<T>(services);
            Configure?.Invoke(mod);
            return mod;
        }
    }
}
