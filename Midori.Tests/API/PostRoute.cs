using Midori.API.Components;
using Midori.Networking;

namespace Midori.Tests.API;

public class PostRoute : IAPIRoute<APIInteraction>
{
    public string RoutePath => "/method";
    public HttpMethod Method => HttpMethod.Post;

    public async Task Handle(APIInteraction interaction)
    {
        await interaction.ReplyMessage(HttpStatusCode.OK, "posting");
    }
}
