using System.Buffers.Binary;
using System.Text;

namespace Midori.Utils.Extensions;

public static class StreamExtensions
{
    public static bool EqualTo(this int value, char c, Action<int>? before = null)
    {
        before?.Invoke(value);
        return value == c;
    }

    #region Reading

    public static byte[] ReadBytes(this Stream stream, long length)
    {
        var buffer = new byte[length];

        var offset = 0;
        var retry = 0;

        while (offset < length && retry < 8)
        {
            var read = stream.Read(buffer, offset, (int)(length - offset));
            offset += read;

            if (read == 0)
                retry++;
        }

        if (retry == 8)
            throw new IOException("Too many read attempts.");

        return buffer;
    }

    public static short ReadInt16(this Stream stream, bool bigEndian = false)
    {
        var bytes = stream.ReadBytes(2);
        return bigEndian ? BinaryPrimitives.ReadInt16BigEndian(bytes) : BitConverter.ToInt16(bytes, 0);
    }

    public static int ReadInt32(this Stream stream, bool bigEndian = false)
    {
        var bytes = stream.ReadBytes(4);
        return bigEndian ? BinaryPrimitives.ReadInt32BigEndian(bytes) : BitConverter.ToInt32(bytes, 0);
    }

    public static long ReadInt64(this Stream stream, bool bigEndian = false)
    {
        var bytes = stream.ReadBytes(8);
        return bigEndian ? BinaryPrimitives.ReadInt64BigEndian(bytes) : BitConverter.ToInt64(bytes, 0);
    }

    public static ushort ReadUInt16(this Stream stream, bool bigEndian = false)
    {
        var bytes = stream.ReadBytes(2);
        return bigEndian ? BinaryPrimitives.ReadUInt16BigEndian(bytes) : BitConverter.ToUInt16(bytes, 0);
    }

    public static uint ReadUInt32(this Stream stream, bool bigEndian = false)
    {
        var bytes = stream.ReadBytes(4);
        return bigEndian ? BinaryPrimitives.ReadUInt32BigEndian(bytes) : BitConverter.ToUInt32(bytes, 0);
    }

    public static ulong ReadUInt64(this Stream stream, bool bigEndian = false)
    {
        var bytes = stream.ReadBytes(8);
        return bigEndian ? BinaryPrimitives.ReadUInt64BigEndian(bytes) : BitConverter.ToUInt64(bytes, 0);
    }

    public static string ReadStringNullByte(this Stream stream, Encoding? enc = null)
    {
        enc ??= Encoding.UTF8;

        var len = stream.ReadByte();
        var buffer = stream.ReadBytes(len);
        stream.ReadByte();
        return enc.GetString(buffer);
    }

    public static string ReadStringNull(this Stream stream, Encoding? enc = null)
    {
        enc ??= Encoding.UTF8;

        var len = stream.ReadUInt32();
        var buffer = stream.ReadBytes(len);
        stream.ReadByte();
        return enc.GetString(buffer);
    }

    public static void AlignRead(this Stream s, uint n, int p)
    {
        var pad = (p - n % p) % p;
        if (pad > 0) s.ReadBytes(pad);
    }

    #endregion

    #region Writing

    /// <summary>
    /// writes a null-terminated string with a byte prefix
    /// </summary>
    public static void WriteStringNullByte(this BinaryWriter w, string str, Encoding? enc = null)
    {
        enc ??= Encoding.UTF8;

        var bytes = enc.GetBytes(str);
        w.Write((byte)bytes.Length);
        w.Write(bytes);
        w.Write((byte)0);
    }

    /// <summary>
    /// writes a null-terminated string with a byte prefix
    /// </summary>
    public static void WriteStringNullByte(this Stream stream, string str, Encoding? enc = null)
    {
        using var bw = new BinaryWriter(stream);
        bw.WriteStringNullByte(str, enc);
    }

    /// <summary>
    /// writes a null-terminated string with an int prefix
    /// </summary>
    public static void WriteStringNull(this BinaryWriter w, string str, Encoding? enc = null)
    {
        enc ??= Encoding.UTF8;

        var bytes = enc.GetBytes(str);
        w.Write(bytes.Length);
        w.Write(bytes);
        w.Write((byte)0);
    }

    /// <summary>
    /// writes a null-terminated string with an int prefix
    /// </summary>
    public static void WriteStringNull(this Stream stream, string str, Encoding? enc = null)
    {
        using var bw = new BinaryWriter(stream);
        bw.WriteStringNull(str, enc);
    }

    public static void PadTo(this BinaryWriter bw, int p) => bw.BaseStream.PadTo(p);

    public static void PadTo(this Stream stream, int p)
    {
        while (stream.Position % p != 0)
            stream.WriteByte(0);
    }

    #endregion

    public static byte ReverseBits(this byte value)
    {
        byte result = 0;

        for (int i = 0; i < 8; i++)
        {
            result = (byte)((result << 1) | (value & 1));
            value >>= 1;
        }

        return result;
    }
}
