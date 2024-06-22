using System.Buffers.Binary;

public class Message
{
    public enum Id
    {
        Choke,
        Unchoke,
        Interested,
        NotInterested,
        Have,
        Bitfield,
        Request,
        Piece,
        Cancel,
        Port,
    }

    public Id? MessageId;
    public byte[]? Payload;

    public Message() {}
    public Message(Id id, byte[] payload)
    {
        MessageId = id;
        Payload = payload;
    }

    public byte[] Serialize()
    {
        if (MessageId == null)
            return new byte[4]; // keepalive

        // <length prefix><message ID><payload>
        var len = 1 + (Payload?.Length ?? 0); // id + payload
        var buf = new byte[4 + len];
        BinaryPrimitives.WriteInt32BigEndian(buf.AsSpan(0,4), len);
        buf[4] = (byte)MessageId;
        if (Payload != null)
            Array.Copy(Payload, 0, buf, 5, Payload.Length);

        return buf;
    }
}
