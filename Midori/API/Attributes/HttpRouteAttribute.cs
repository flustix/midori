using JetBrains.Annotations;
using Midori.API.Components;

namespace Midori.API.Attributes;

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public class HttpRouteAttribute : Attribute
{
    public string Path { get; }
    public APIMethod Method { get; }

    public HttpRouteAttribute(string path, APIMethod method = APIMethod.Get)
    {
        Path = path;
        Method = method;
    }
}
