using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;
using Midori.Logging;
using Midori.Networking.WebSockets.Frame;
using Midori.Utils.Extensions;

namespace Midori.Networking.WebSockets;

public abstract class WebSocket : IDisposable
{
    protected abstract bool MaskData { get; }

    private const int chunk_size = 1016;

    public event Action? OnOpen;
    public event Action? OnClose;
    public event Action<WebSocketMessage>? OnMessage;

    private volatile WebSocketState state = WebSocketState.None;
    private readonly object stateLock = new { };

    public WebSocketState State
    {
        get
        {
            lock (stateLock)
            {
                return state;
            }
        }
        protected set
        {
            lock (stateLock)
            {
                state = value;
            }
        }
    }

    internal Stream Stream { get; set; } = null!;

    internal WebSocket(Stream stream)
    {
        Stream = stream;
    }

    internal WebSocket()
    {
    }

    protected void Open()
    {
        State = WebSocketState.Open;

        try
        {
            OnOpen?.Invoke();
        }
        catch (Exception e)
        {
            Logger.Error(e, "Failed to invoke OnOpen!", LoggingTarget.Network);
        }

        startReceive();
    }

    private void startReceive()
    {
        try
        {
            var op = WebSocketOpcode.Continuation;
            var buffer = new MemoryStream();

            while (State == WebSocketState.Open)
            {
                var frame = WebSocketFrame.ReadFrame(Stream);

                switch (frame.Opcode)
                {
                    case WebSocketOpcode.Continuation:
                    case WebSocketOpcode.Text:
                    case WebSocketOpcode.Binary:
                    {
                        if (frame.Opcode != WebSocketOpcode.Continuation)
                            op = frame.Opcode;

                        var data = frame.Payload;
                        buffer.Write(data, 0, data.Length);

                        if (frame.IsPartial)
                            continue;

                        var bytes = buffer.ToArray();
                        var message = new WebSocketMessage(op, bytes);

                        try
                        {
                            OnMessage?.Invoke(message);
                        }
                        catch (Exception e)
                        {
                            Logger.Error(e, "Failed to invoke OnMessage!", LoggingTarget.Network);
                        }

                        buffer.Dispose();
                        buffer = new MemoryStream();
                        op = WebSocketOpcode.Continuation;

                        break;
                    }

                    case WebSocketOpcode.Close:
                    {
                        var code = frame.ClosureCode;
                        close(code, !code.IsReservedCode(), true);
                        break;
                    }

                    case WebSocketOpcode.Ping:
                        break;

                    case WebSocketOpcode.Pong:
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        catch (Exception ex)
        {
            switch (ex)
            {
                case ObjectDisposedException or IOException or SocketException:
                    close(WebSocketCloseCode.AbnormalClosure, false, false);
                    return;

                default:
                    throw;
            }
        }
    }

    #region Sending

    public async Task<bool> SendTextAsync(string text) => await Task.Run(() => SendText(text));
    public async Task<bool> SendBinaryAsync(byte[] data) => await Task.Run(() => SendBinary(data));

    public bool SendText(string text) => sendData(Encoding.UTF8.GetBytes(text), WebSocketOpcode.Text);
    public bool SendBinary(byte[] data) => sendData(data, WebSocketOpcode.Binary);

    private bool sendData(byte[] data, WebSocketOpcode code)
    {
        var length = data.Length;
        var sections = length / chunk_size;

        if (length % chunk_size != 0)
            sections++;

        for (var i = 0; i < sections; i++)
        {
            var start = i * chunk_size;
            var end = Math.Min(start + chunk_size, length);
            var chunk = data[start..end];
            var fin = i == sections - 1 ? WebSocketFinal.Final : WebSocketFinal.Partial;
            var opcode = i == 0 ? code : WebSocketOpcode.Continuation;
            var frame = new WebSocketFrame(fin, opcode, chunk);

            if (!sendFrame(frame))
                return false;
        }

        return true;
    }

    private bool sendFrame(WebSocketFrame frame)
    {
        try
        {
            var bytes = frame.ToArray();
            Stream.Write(bytes, 0, bytes.Length);
        }
        catch (Exception e)
        {
            Logger.Error(e, $"Failed to send bytes! [{State}]", LoggingTarget.Network);
            return false;
        }

        return true;
    }

    #endregion

    #region Closing

    public void Close(WebSocketCloseCode code, string message) => close(code, true, false);

    private void close(WebSocketCloseCode code, bool send, bool receive)
    {
        if (State is WebSocketState.Closing or WebSocketState.Closed)
            return;

        send &= State == WebSocketState.Open;

        State = WebSocketState.Closing;

        if (send)
        {
            var codeBytes = new byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(codeBytes, (ushort)code);

            var frame = new WebSocketFrame(WebSocketFinal.Final, WebSocketOpcode.Close, codeBytes);
            sendFrame(frame);
        }

        State = WebSocketState.Closed;

        try
        {
            OnClose?.Invoke();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Exception while invoking OnClose.", LoggingTarget.Network);
        }

        Dispose();
    }

    #endregion

    public virtual void Dispose()
    {
        Stream.Dispose();
    }

    public override string ToString() => $"{State} {Stream}";
}
