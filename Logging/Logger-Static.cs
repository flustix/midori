using Midori.Utils;

namespace Midori.Logging;

#nullable disable

public partial class Logger
{
    public static LogLevel Level { get; set; } = RuntimeUtils.IsDebugBuild ? LogLevel.Debug : LogLevel.Verbose;
    public static string LogDirectory { get; set; } = "logs";

    private static readonly object static_sync_lock = new();
    private static readonly object flush_sync_lock = new();
    private static readonly Dictionary<string, Logger> static_loggers = new();

    private static readonly HashSet<string> reserved_names = new(Enum.GetNames<LoggingTarget>().Select(n => n.ToLowerInvariant()));

    #region Writing (public)

    public static void Error(Exception e, string description, LoggingTarget target = LoggingTarget.General, bool recursive = false)
    {
        error(e, description, target, null, recursive);
    }

    public static void Error(Exception e, string description, string name, bool recursive = false)
    {
        error(e, description, null, name, recursive);
    }

    public static void Log(string message, LoggingTarget target = LoggingTarget.General, LogLevel level = LogLevel.Verbose)
    {
        log(message, target, null, level);
    }

    #endregion

    #region Writing (private)

    private static void error(Exception e, string description, LoggingTarget? target, string name, bool recursive)
    {
        log($"{description}", target, name, LogLevel.Error, e);

        if (recursive && e.InnerException != null)
            error(e.InnerException, $"{description} (inner)", target, name, true);
    }

    private static void log(string message, LoggingTarget? target, string loggerName, LogLevel level, Exception exception = null)
    {
        try
        {
            if (target.HasValue)
                GetLogger(target.Value).Add(message, level, exception);
            else
                GetLogger(loggerName).Add(message, level, exception);
        }
        catch
        {
        }
    }

    #endregion

    #region Getting loggers

    public static Logger GetLogger(LoggingTarget target = LoggingTarget.General) => GetLogger(target.ToString());

    public static Logger GetLogger(string name)
    {
        lock (static_sync_lock)
        {
            string nameLower = name.ToLowerInvariant();

            if (!static_loggers.TryGetValue(nameLower, out Logger l))
                static_loggers[nameLower] = l = Enum.TryParse(name, true, out LoggingTarget target) ? new Logger(target) : new Logger(name, true);

            return l;
        }
    }

    #endregion

    #region Colors

    private static ConsoleColor getColor(LogLevel? logLevel) => logLevel switch
    {
        LogLevel.Warning => ConsoleColor.Yellow,
        LogLevel.Error => ConsoleColor.Red,
        LogLevel.Verbose => ConsoleColor.Cyan,
        LogLevel.Debug => ConsoleColor.Magenta,
        _ => ConsoleColor.White
    };

    private static ConsoleColor getColor(LoggingTarget? target) => target switch
    {
        LoggingTarget.Database => ConsoleColor.Green,
        LoggingTarget.Network => ConsoleColor.Magenta,
        _ => ConsoleColor.Blue
    };

    #endregion
}

public enum LogLevel
{
    Debug,
    Verbose,
    Warning,
    Error
}

public enum LoggingTarget
{
    Info, // only logs in console and not in file
    General,
    Network,
    Database
}
