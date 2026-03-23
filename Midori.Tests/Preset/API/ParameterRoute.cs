using System.Net.Http;
using System.Threading.Tasks;
using Midori.API.Components;
using Midori.Networking;

namespace Midori.Tests.Preset.API;

public class ParameterRoute : IAPIRoute<APIInteraction>
{
    public string RoutePath => "/param/:id";
    public HttpMethod Method => HttpMethod.Get;

    public async Task Handle(APIInteraction interaction)
    {
        await interaction.ReplyMessage(HttpStatusCode.OK, interaction.GetStringParameter("id")!);
    }
}
