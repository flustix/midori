using System.Reflection;
using Midori.API.Components;

namespace Midori.Networking.Middleware;

/// <summary>
/// A middleware that injects after authentication and before method parameter parsing.
/// </summary>
public interface IParameterMiddleware : IMiddleware
{
    /// <param name="ctx">The raw request.</param>
    /// <param name="controller">The controller instance.</param>
    /// <param name="method">The method that will be called.</param>
    /// <param name="data">All parameter-related data for this request.</param>
    /// <returns>An optional error message. Return null if successful.</returns>
    APIReturn<object>? Handle(HttpServerContext ctx, object controller, MethodInfo method, Data data);

    public class Data
    {
        /// <summary>
        /// Path-based parameters.
        /// </summary>
        public Dictionary<string, string> RawPath { get; }

        /// <summary>
        /// Parameters provided by the authentication provider. (If there is one)
        /// </summary>
        public Dictionary<string, object> Auth { get; }

        public Data(Dictionary<string, string> rawPath, Dictionary<string, object> auth)
        {
            RawPath = rawPath;
            Auth = auth;
        }
    }
}
