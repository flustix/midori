namespace Midori.DBus.Exceptions;

public class DBusException : Exception
{
    public DBusException(string text)
        : base(text)
    {
    }
}
