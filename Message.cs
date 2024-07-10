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

    public static Message Have(int pieceId)
    {
        var payload = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(payload, (uint)pieceId);
        return new Message(Id.Have, payload);
    }

    public byte[] Serialize()
    {
        if (MessageId == null)
            return new byte[4]; // keepalive

        // <length prefix><message ID><payload>
        var len = 1 + (Payload?.Length ?? 0); // id + payload
        var buf = new byte[4 + len];
        BinaryPrimitives.WriteUInt32BigEndian(buf.AsSpan(0,4), (uint)len);
        buf[4] = (byte)MessageId;
        if (Payload != null)
            Array.Copy(Payload, 0, buf, 5, Payload.Length);

        return buf;
    }

    public override string ToString()
    {
        switch (MessageId)
        {
            case null:
                return "KeepAlive:0";
            case Id.Bitfield:
            case Id.Choke:
            case Id.Unchoke:
            case Id.Piece:
                var index = BinaryPrimitives.ReadUInt32BigEndian(Payload.AsSpan()[0..4]);
                return $"{MessageId}@{index}:{Payload?.Length}";
            default:
                return $"{MessageId}:{BitConverter.ToString(Payload ?? [])}";
        }
    }

    public static Message Request(int index, int begin, int length)
    {
        var payload = new byte[12];
        BinaryPrimitives.WriteInt32BigEndian(payload.AsSpan(0,4), index);
        BinaryPrimitives.WriteInt32BigEndian(payload.AsSpan(4,4), begin);
        BinaryPrimitives.WriteInt32BigEndian(payload.AsSpan(8,4), length);
        return new Message(Id.Request, payload);
    }
}
