namespace Midori.DBus.Exceptions;

public class DBusFailureException : DBusException
{
    public DBusFailureException(string text)
        : base(text)
    {
    }
}
