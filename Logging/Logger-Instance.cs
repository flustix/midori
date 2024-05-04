using System.Globalization;
using Midori.Utils;

namespace Midori.Logging;

#nullable disable

public partial class Logger
{
    public LoggingTarget? Target { get; }
    public string Name { get; }
    public string Filename { get; }

    private bool headerAdded;
    private readonly Queue<string> pendingFileOutput = new();

    private Logger(LoggingTarget target = LoggingTarget.General)
        : this(target.ToString(), false)
    {
        Target = target;
    }

    private Logger(string name, bool checkedReserved)
    {
        var lower = name.ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(lower))
            throw new ArgumentException("The name of a logger must be non-null and may not contain only white space.", nameof(name));

        if (checkedReserved && Logger.reserved_names.Contains(lower))
            throw new ArgumentException($"The name \"{name}\" is reserved. Please use the {nameof(LoggingTarget)}-value corresponding to the name instead.");

        Name = name;
        Filename = $"{lower}.log";
    }

    public void Add(string message = "", LogLevel level = LogLevel.Verbose, Exception exception = null) =>
        add(message, level, exception);

    private void add(string message = "", LogLevel level = LogLevel.Verbose, Exception exception = null)
    {
        if (level < Logger.Level)
            return;

        string logOutput = message;

        if (exception != null)
            logOutput += $"\n{exception}";

        var lines = logOutput.Replace(@"\r\n", @"\n").Split('\n')
                             .Select(s => $"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)} [{level.ToString().ToLowerInvariant()}]: {s.Trim()}");

        writeToConsole(logOutput, level);

        if (Target == LoggingTarget.Info)
            return;

        lock (Logger.flush_sync_lock)
        {
            lock (pendingFileOutput)
            {
                foreach (string l in lines)
                    pendingFileOutput.Enqueue(l);
            }

            writePendingLines();
        }
    }

    private void writeToConsole(string message, LogLevel level)
    {
        var target = (Target?.ToString() ?? Name).PadRight(10)[..10];
        var severity = level.ToString().PadRight(8)[..8];

        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write($"[{DateTime.Now:HH:mm:ss}] ");
        Console.ForegroundColor = getColor(Target);
        Console.Write($"{target} ");
        Console.ForegroundColor = getColor(level);
        Console.Write($"{severity} ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"{message}");
    }

    private void writePendingLines()
    {
        string[] lines;

        lock (pendingFileOutput)
        {
            lines = pendingFileOutput.ToArray();
            pendingFileOutput.Clear();
        }

        try
        {
            var logsDir = LogDirectory;

            if (!Directory.Exists(logsDir))
                Directory.CreateDirectory(logsDir);

            using var stream = File.Open(Path.Combine(logsDir, Filename), FileMode.Append, FileAccess.Write, FileShare.Read);
            using var writer = new StreamWriter(stream);

            if (!headerAdded)
            {
                writer.WriteLine("----------------------------------------------------------");
                writer.WriteLine($"{Name} Log (LogLevel: {Level})");
                writer.WriteLine($"Environment: {RuntimeUtils.OS} ({Environment.OSVersion}), {Environment.ProcessorCount} cores ");
                writer.WriteLine("----------------------------------------------------------");

                headerAdded = true;
            }

            foreach (string line in lines)
                writer.WriteLine(line);
        }
        catch
        {
        }
    }
}
