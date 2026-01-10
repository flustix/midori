using System.Runtime.InteropServices;

namespace Midori.DBus;

internal static class DBusEnv
{
    internal static uint UserID => geteuid();

    [DllImport("libc")]
    internal static extern uint geteuid();
}
