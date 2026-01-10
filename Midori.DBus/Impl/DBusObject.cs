using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Midori.DBus.TypeImpls")]
[assembly: InternalsVisibleTo("Midori.DBus.Tests")]

namespace Midori.DBus.Impl;

internal class DBusObject
{
    public string Destination { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
}
