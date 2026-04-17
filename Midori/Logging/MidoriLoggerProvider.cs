using Microsoft.Extensions.Logging;

namespace Midori.Logging;

public class MidoriLoggerProvider : ILoggerProvider
{
    public static readonly string GENERAL = nameof(LoggingTarget.General);
    public static readonly string NETWORK = nameof(LoggingTarget.Network);
    public static readonly string DATABASE = nameof(LoggingTarget.Database);

    public ILogger CreateLogger(string categoryName)
    {
        if (categoryName.Contains('.'))
            categoryName = categoryName[(categoryName.LastIndexOf('.') + 1)..];

        return Enum.TryParse<LoggingTarget>(categoryName, out var target)
            ? Logger.GetLogger(target)
            : Logger.GetLogger(categoryName);
    }

    public void Dispose()
    {
    }
}
