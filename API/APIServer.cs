using System.Net;
using System.Reflection;
using Midori.API.Components;
using Midori.API.Components.Interfaces;
using Midori.Logging;

namespace Midori.API;

public class APIServer<T>
    where T : APIInteraction, new()
{
    private Logger logger { get; } = Logger.GetLogger("API");

    private List<IAPIRoute<T>> routeList { get; } = new();
    private HttpListener? listener;

    public bool Running { get; private set; }
    public bool ShowTimings { get; set; }

    public string InternalError { get; set; } = "Welp, something went very wrong. It's probably not your fault, but please report this to the developers.";
    public string NotFoundError { get; set; } = "The requested route does not exist.";

    public void Start(int port)
    {
        Running = true;

        listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{port}/");
        listener.Start();

        var thread = new Thread(startListener);
        thread.Start();

        logger.Add($"Started API server on port {port}.");
    }

    public void Stop()
    {
        Running = false;

        listener?.Stop();
        listener?.Close();

        listener = null;
        routeList.Clear();

        logger.Add("Stopped API server.");
    }

    public void AddRoutesFromAssembly<U>(Assembly assembly)
        where U : IAPIRoute<T>
    {
        assembly.GetTypes()
                .Where(t => t.GetInterfaces().Contains(typeof(U)))
                .ToList()
                .ForEach(t =>
                {
                    var route = (U)Activator.CreateInstance(t)!;
                    routeList.Add(route);
                });

        routeList.Sort((a, b) => string.Compare(a.RoutePath, b.RoutePath, StringComparison.Ordinal));
        routeList.ForEach(r => logger.Add($"Loaded API route {r.Method.Method} {r.RoutePath}"));
    }

    private void startListener(object? o)
    {
        while (Running)
            process();
    }

    private void process()
    {
        var res = listener?.BeginGetContext(handle, listener);
        res?.AsyncWaitHandle.WaitOne();
    }

    private async void handle(IAsyncResult result)
    {
        try
        {
            var context = listener?.EndGetContext(result);
            if (context == null) return;

            var req = context.Request;
            var res = context.Response;

            IAPIRoute<T>? route = default;
            Dictionary<string, string> parameters = new();

            foreach (var handler in routeList)
            {
                if (!string.Equals(req.HttpMethod, handler.Method.Method, StringComparison.InvariantCultureIgnoreCase))
                    continue;

                // don't even know how this would happen but ok
                if (req.Url == null)
                    continue;

                var url = req.Url.AbsolutePath;

                if (handler.RoutePath == url && !handler.RoutePath.Contains(':'))
                {
                    route = handler; // exact match with no parameters
                    break;
                }

                var parts = handler.RoutePath.Split('/');
                var reqParts = url.Split('/');

                if (parts.Length == 0 || reqParts.Length == 0)
                    continue;

                if (reqParts.Last() == "")
                    reqParts = reqParts[..^1]; // remove trailing slash (if any)

                if (parts.Length != reqParts.Length)
                    continue;

                var match = true;
                Dictionary<string, string> reqParams = new();

                for (var i = 0; i < parts.Length; i++)
                {
                    if (parts[i].StartsWith(':'))
                    {
                        reqParams.Add(parts[i][1..], reqParts[i]);
                    }
                    else if (!parts[i].Equals(reqParts[i]))
                    {
                        match = false;
                        break;
                    }
                }

                if (!match) continue;

                route = handler;
                parameters = reqParams;
            }

            var interaction = new T();
            interaction.Populate(req, res, parameters);

            if (route == null)
            {
                await interaction.ReplyError(HttpStatusCode.NotFound, NotFoundError);
                return;
            }

            try
            {
                var authHandler = interaction as IHasAuthorizationInfo;

                if (route is INeedsAuthorization)
                {
                    if (authHandler == null)
                        throw new InvalidOperationException("Route requires authorization but interaction does not have authorization info.");

                    if (!authHandler.IsAuthorized)
                    {
                        await interaction.ReplyError(HttpStatusCode.Unauthorized, authHandler.AuthorizationError);
                        return;
                    }
                }

                if (ShowTimings)
                    interaction.StartTimer();

                await route.Handle(interaction);
            }
            catch (Exception ex)
            {
                await interaction.ReplyError(HttpStatusCode.InternalServerError, InternalError, ex);
                logger.Add("Error handling route", LogLevel.Error, ex);
            }
        }
        catch (Exception ex)
        {
            logger.Add("Error handling request", LogLevel.Error, ex);
        }
    }
}
