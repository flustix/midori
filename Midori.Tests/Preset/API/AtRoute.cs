using System.Net.Http;
using System.Threading.Tasks;
using Midori.API.Components;
using Midori.Networking;

namespace Midori.Tests.Preset.API;

public class AtRoute : IAPIRoute<APIInteraction>
{
    public string RoutePath => "/@me";
    public HttpMethod Method => HttpMethod.Get;

    public async Task Handle(APIInteraction interaction)
    {
        await interaction.ReplyMessage(HttpStatusCode.OK, "ok");
    }
}
