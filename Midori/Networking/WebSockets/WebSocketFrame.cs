using System.Buffers.Binary;
using System.Text;
using Midori.Networking.WebSockets.Frame;
using Midori.Utils.Extensions;

namespace Midori.Networking.WebSockets;

public class WebSocketFrame
{
    private WebSocketFinal final;
    private WebSocketRsv r1;
    private WebSocketRsv r2;
    private WebSocketRsv r3;
    private ulong length;
    private bool mask;
    private byte[]? maskingKey;
    private byte[]? payload;

    public bool IsPartial => final == WebSocketFinal.Partial;
    public WebSocketOpcode Opcode { get; private init; }

    public int Length => (int)length;
    public byte[] Payload => payload?.ToArray() ?? Array.Empty<byte>();

    public bool IsText => Opcode == WebSocketOpcode.Text;
    public bool IsBinary => Opcode == WebSocketOpcode.Binary;

    public WebSocketCloseCode ClosureCode
    {
        get
        {
            if (payload == null || payload.Length < 2)
                return WebSocketCloseCode.NoStatusRcvd;

            return (WebSocketCloseCode)BinaryPrimitives.ReadUInt16BigEndian(payload.AsSpan()[..2]);
        }
    }

    public string CloseReason
    {
        get
        {
            if (payload == null || payload.Length < 3)
                return "";

            var buffer = payload[1..^1];
            return Encoding.UTF8.GetString(buffer);
        }
    }

    private WebSocketFrame()
    {
    }

    internal WebSocketFrame(WebSocketFinal fin, WebSocketOpcode opcode, byte[] payload)
    {
        final = fin;
        Opcode = opcode;
        this.payload = payload;
        length = (ulong)payload.Length;
    }

    internal static WebSocketFrame ReadFrame(Stream stream)
    {
        var frame = processHeader(stream.ReadBytes(2));
        processLength(frame, stream);

        if (frame.mask)
            frame.maskingKey = stream.ReadBytes(4);

        if (frame.length > 0)
        {
            frame.payload = stream.ReadBytes((int)frame.length);
            frame.UnmaskPayload();
        }

        return frame;
    }

    private static WebSocketFrame processHeader(byte[] head) => new()
    {
        final = (WebSocketFinal)(head[0] >> 7),
        r1 = (WebSocketRsv)((head[0] >> 4) & 0b111),
        r2 = (WebSocketRsv)((head[0] >> 3) & 0b111),
        r3 = (WebSocketRsv)((head[0] >> 2) & 0b111),
        Opcode = (WebSocketOpcode)(head[0] & 0b1111),
        mask = head[1] >> 7 == 1,
        length = (ulong)(head[1] & 0b1111111)
    };

    private static void processLength(WebSocketFrame frame, Stream stream) => frame.length = frame.length switch
    {
        126 => stream.ReadUInt16(true),
        127 => stream.ReadUInt64(true),
        _ => frame.length
    };

    public byte[] ToArray()
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        bw.Write((byte)(((byte)final << 7) | ((byte)r1 << 4) | ((byte)r2 << 3) | ((byte)r3 << 2) | (byte)Opcode));
        bw.Write((byte)((uint)(mask ? 1 << 7 : 0) | (length < 126 ? (uint)length : length < 65536 ? 126 : 127)));

        if (length > 125)
        {
            var big = length >= 65536;
            var dest = new byte[big ? 8 : 2];

            if (big)
                BinaryPrimitives.WriteUInt64BigEndian(dest, length);
            else
                BinaryPrimitives.WriteUInt16BigEndian(dest, (ushort)length);

            bw.Write(dest);
        }

        if (mask)
            bw.Write(maskingKey!);

        if (payload != null)
            bw.Write(payload);

        return ms.ToArray();
    }

    public void MaskPayload()
    {
        if (mask)
            return;

        maskingKey = new byte[4];
        Random.Shared.NextBytes(maskingKey);

        payload = payload?.Select((b, i) => (byte)(b ^ maskingKey![i % 4])).ToArray();
        mask = true;
    }

    public void UnmaskPayload()
    {
        if (!mask)
            return;

        payload = payload?.Select((b, i) => (byte)(b ^ maskingKey![i % 4])).ToArray();
        mask = false;
        maskingKey = null;
    }

    public override string ToString()
    {
        return $"Final: {final}, RSV1: {r1}, RSV2: {r2}, RSV3: {r3}, Opcode: {Opcode}, Mask: {mask}, Length: {length}, Payload: {payload?.Length}";
    }
}
