using System.Net;
using Midori.API.Components;
using Midori.Networking.Collections;

namespace Midori.Networking;

public class HttpConfiguration
{
    public IPAddress Address { get; set; } = IPAddress.Loopback;
    public ushort Port { get; set; } = 8080;

    /// <summary>
    /// Handles options requests automatically.
    /// </summary>
    public bool AutoHandleOptions { get; set; } = true;

    /// <summary>
    /// Automatically sets the response "Access-Control-Allow-Origin" to the request's "Origin" header.
    /// </summary>
    public bool AssignRequestOriginToCors { get; set; } = true;

    public string[] AllowedOrigins { get; set; } = { "localhost" };
    public string[] AllowedMethods { get; set; } = Enum.GetValues<APIMethod>().Select(x => x.ToString().ToUpperInvariant()).ToArray();

    public string[] AllowedHeaders { get; set; } =
    {
        "Accept",
        "Accept-Encoding",
        "Accept-Language",
        "Authorization",
        "Connection",
        "Content-Type",
        "Cookie",
        "Host",
        "Origin",
        "Referer",
        "X-Requested-With",
        "X-Forwarded-For",
        "User-Agent"
    };

    public void ApplyHeaders(HttpHeaderCollection response, HttpHeaderCollection? request = null)
    {
        response.Add("Access-Control-Allow-Origin", AssignRequestOriginToCors ? request?.Get("Origin") ?? "*" : string.Join(", ", AllowedOrigins));
        response.Add("Access-Control-Allow-Methods", string.Join(", ", AllowedMethods));
        response.Add("Access-Control-Allow-Headers", string.Join(", ", AllowedHeaders));
    }
}
