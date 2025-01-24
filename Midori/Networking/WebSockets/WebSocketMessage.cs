using System.Text;
using Midori.Networking.WebSockets.Frame;

namespace Midori.Networking.WebSockets;

public class WebSocketMessage
{
    public WebSocketOpcode OpCode { get; private init; }
    public byte[] Payload { get; private init; }

    public bool IsText => OpCode == WebSocketOpcode.Text;

    public string Text
    {
        get
        {
            if (!IsText)
                throw new InvalidOperationException("This message is not a text message.");

            return Encoding.UTF8.GetString(Payload);
        }
    }

    public WebSocketMessage(WebSocketOpcode opcode, byte[] payload)
    {
        OpCode = opcode;
        Payload = payload;
    }
}
