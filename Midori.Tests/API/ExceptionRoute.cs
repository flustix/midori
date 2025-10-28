using Midori.API.Components;

namespace Midori.Tests.API;

public class ExceptionRoute : IAPIRoute<APIInteraction>
{
    public string RoutePath => "/exception";
    public HttpMethod Method => HttpMethod.Get;

    public Task Handle(APIInteraction interaction)
    {
        throw new Exception("testing exception");
    }
}
