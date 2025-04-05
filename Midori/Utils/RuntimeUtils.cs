using System.Diagnostics;
using System.Reflection;

namespace Midori.Utils;

public class RuntimeUtils
{
    public static Platform OS { get; }
    public static bool IsDebugBuild => isDebugBuild.Value;

    static RuntimeUtils()
    {
        if (OperatingSystem.IsWindows())
            OS = Platform.Windows;
        if (OperatingSystem.IsLinux())
            OS = Platform.Linux;

        if (OS == 0)
            OS = Platform.Unknown;
    }

    private static Lazy<bool> isDebugBuild { get; } = new(() =>
        isDebugAssembly(typeof(RuntimeUtils).Assembly) || isDebugAssembly(Assembly.GetEntryAssembly() ?? throw new InvalidOperationException("Entry assembly could not be detected.")));

    private static bool isDebugAssembly(Assembly? assembly) => assembly?.GetCustomAttributes(false).OfType<DebuggableAttribute>().Any(da => da.IsJITTrackingEnabled) ?? false;

    public enum Platform
    {
        Windows = 1,
        Linux = 2,
        Unknown = 99
    }
}
