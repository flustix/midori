namespace Midori.DBus.Exceptions;

public class DBusServiceUnknownException : DBusException
{
    public DBusServiceUnknownException(string text)
        : base(text)
    {
    }
}
