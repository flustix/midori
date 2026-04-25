using Midori.Networking;

namespace Midori.API;

public interface IAPIAuthenticator
{
    bool Authenticate(HttpServerContext ctx, out List<string> scopes, out Dictionary<string, object> data);
}
