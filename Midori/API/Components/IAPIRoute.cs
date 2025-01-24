using JetBrains.Annotations;

namespace Midori.API.Components;

[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public interface IAPIRoute<in T>
    where T : APIInteraction
{
    string RoutePath { get; }
    HttpMethod Method { get; }
    Task Handle(T interaction);
}
