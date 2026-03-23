using System.Net.Http;
using System.Threading.Tasks;
using Midori.API.Components;
using Midori.Networking;

namespace Midori.Tests.Preset.API;

public class PatchRoute : IAPIRoute<APIInteraction>
{
    public string RoutePath => "/method";
    public HttpMethod Method => HttpMethod.Patch;

    public async Task Handle(APIInteraction interaction)
    {
        await interaction.ReplyMessage(HttpStatusCode.OK, "patching");
    }
}
