using JetBrains.Annotations;

namespace Midori.API.Components;

[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public interface IAPIRoute<in T>
    where T : APIInteraction
{
    public string RoutePath { get; }
    public HttpMethod Method { get; }
    public Task Handle(T interaction);
}
