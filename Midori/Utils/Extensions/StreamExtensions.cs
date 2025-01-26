using System.Buffers.Binary;

namespace Midori.Utils.Extensions;

public static class StreamExtensions
{
    public static bool EqualTo(this int value, char c, Action<int>? before = null)
    {
        before?.Invoke(value);
        return value == c;
    }

    public static byte[] ReadBytes(this Stream stream, int length)
    {
        var buffer = new byte[length];

        var offset = 0;
        var retry = 0;

        while (offset < length && retry < 8)
        {
            offset += stream.Read(buffer, offset, length - offset);

            if (offset < length)
                retry++;
        }

        if (retry == 8)
            throw new IOException("Too many read attempts.");

        return buffer;
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
