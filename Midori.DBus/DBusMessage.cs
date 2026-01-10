using Midori.DBus.IO;
using Midori.DBus.Values;
using Midori.Utils.Extensions;

namespace Midori.DBus;

public class DBusMessage
{
    public DBusEndian Endian { get; private set; }
    public DBusMessageType Type { get; private set; }
    public int Flags { get; private set; }
    public int Version { get; private set; }
    public uint Serial { get; private set; }

    public string Destination
    {
        get => Headers.GetValueOrDefault(DBusHeaderID.Destination).AsString() ?? string.Empty;
        set => Headers[DBusHeaderID.Destination] = new DBusStringValue { Value = value };
    }

    public string Path
    {
        get => Headers.GetValueOrDefault(DBusHeaderID.Path).AsObjectPath() ?? string.Empty;
        set => Headers[DBusHeaderID.Path] = new DBusObjectPathValue { Value = value };
    }

    public string Interface
    {
        get => Headers.GetValueOrDefault(DBusHeaderID.Interface).AsString() ?? string.Empty;
        set => Headers[DBusHeaderID.Interface] = new DBusStringValue { Value = value };
    }

    public string Member
    {
        get => Headers.GetValueOrDefault(DBusHeaderID.Member).AsString() ?? string.Empty;
        set => Headers[DBusHeaderID.Member] = new DBusStringValue { Value = value };
    }

    public string Signature
    {
        get => Headers.GetValueOrDefault(DBusHeaderID.Signature).AsSignature() ?? string.Empty;
        set => Headers[DBusHeaderID.Signature] = new DBusSignatureValue { Value = value };
    }

    public byte[] Body { get; }
    public Dictionary<DBusHeaderID, IDBusValue> Headers { get; }

    public DBusMessage(DBusEndian endian, DBusMessageType type, int flags, int version, uint serial, byte[] body, Dictionary<DBusHeaderID, IDBusValue>? headers = null)
    {
        Endian = endian;
        Type = type;
        Flags = flags;
        Version = version;
        Serial = serial;
        Body = body;
        Headers = headers ?? new Dictionary<DBusHeaderID, IDBusValue>();
    }

    public DBusReader GetBodyReader() => new(Body);

    public void SetMethodCall(string dest, string path, string @interface, string member)
    {
        Type = DBusMessageType.MethodCall;
        Destination = dest;
        Path = path;
        Interface = @interface;
        Member = member;
    }

    internal void Write(Stream stream)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        bw.Write((byte)Endian);
        bw.Write((byte)Type);
        bw.Write((byte)Flags);
        bw.Write((byte)Version);
        bw.Write(Body.Length); // body length
        bw.Write(Serial);

        var headPos = ms.Position;
        bw.Write(0); // temp length

        foreach (var (key, val) in Headers)
        {
            bw.PadTo(8);
            bw.Write((byte)key);
            bw.WriteStringNullByte(val.GetSignature());
            val.Write(bw);
        }

        var headEnd = ms.Position;
        var headLen = (int)(headEnd - headPos - 4);
        ms.Position = headPos;
        bw.Write(headLen);
        ms.Position = headEnd;

        bw.PadTo(8);
        bw.Write(Body);

        stream.Write(ms.ToArray());
    }

    internal static DBusMessage ReadMessage(Stream stream)
    {
        var endian = (DBusEndian)stream.ReadByte();
        var type = (DBusMessageType)stream.ReadByte();
        var flags = stream.ReadByte();
        var version = stream.ReadByte();
        var length = stream.ReadUInt32(endian == DBusEndian.Big);
        var serial = stream.ReadUInt32(endian == DBusEndian.Big);
        var headerLen = stream.ReadUInt32(endian == DBusEndian.Big);

        var headerBytes = stream.ReadBytes(headerLen);
        stream.AlignRead(headerLen, 8);

        var headers = readHeaders(headerBytes);
        var body = stream.ReadBytes(length);
        return new DBusMessage(endian, type, flags, version, serial, body, headers);
    }

    private static Dictionary<DBusHeaderID, IDBusValue> readHeaders(byte[] bytes)
    {
        var dict = new Dictionary<DBusHeaderID, IDBusValue>();
        var reader = new DBusReader(bytes);

        while (reader.Stream.Position < reader.Stream.Length)
        {
            reader.Align(8);
            var id = (DBusHeaderID)reader.ReadByte();
            var sig = reader.ReadSignature();

            var val = IDBusValue.GetForSignature(sig);
            val.Read(reader.Stream);
            dict[id] = val;
        }

        return dict;
    }
}

public enum DBusEndian : byte
{
    Little = (byte)'l',
    Big = (byte)'B'
}

public enum DBusMessageType : byte
{
    Invalid = 0,
    MethodCall = 1,
    MethodReturn = 2,
    Error = 3,
    Signal = 4
}

public enum DBusHeaderID : byte
{
    Invalid = 0,
    Path = 1,
    Interface = 2,
    Member = 3,
    ErrorName = 4,
    ReplySerial = 5,
    Destination = 6,
    Sender = 7,
    Signature = 8,
    UnixFds = 9
}
